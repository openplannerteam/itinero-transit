using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Algorithms.CSA
{
    //using LocId = UInt64;
    using Time = UInt64;

    /// <summary>
    /// Calculates the fastest journey from A to B starting at a given time; using CSA (forward A*).
    /// It does _not_ use footpath interlinks (yet)
    /// </summary>
    internal class EarliestConnectionScan<T>
        where T : IJourneyStats<T>
    {
        private readonly List<((uint localTileId, uint localId), Journey<T>)> _userTargetLocations;

        private readonly TransitDb.TransitDbSnapShot _tdb;
        private readonly ConnectionsDb _connectionsProvider;
        private readonly StopsDb _stopsDb;
        private readonly StopsDb.StopsDbReader _stopsReader;

        /// <summary>
        /// The last allowed departure time. Note that scanning could continue after it, if a scan-overshoot is given
        /// </summary>
        private readonly Time _lastArrival;

        public IReadOnlyDictionary<(uint localTileId, uint localId), Journey<T>> Isochrone() => _s;

        public ulong ScanEndTime { get; private set; } = ulong.MinValue;

        public ulong ScanBeginTime { get; }

        private readonly IOtherModeGenerator _transferPolicy, _walkPolicy;

        /// <summary>
        /// This dictionary keeps, for each stop, the journey that arrives as early as possible
        /// </summary>
        private readonly Dictionary<(uint localTileId, uint localId), Journey<T>> _s =
            new Dictionary<(uint localTileId, uint localId), Journey<T>>();

        /// <summary>
        /// Keeps track of where we are on each trip, thus if we wouldn't leave a bus once we're on it
        /// </summary>
        private readonly Dictionary<uint, Journey<T>> _trips = new Dictionary<uint, Journey<T>>();

        public EarliestConnectionScan(
            ScanSettings<T> settings)
        {
            settings.SanityCheck();
            ScanBeginTime = settings.EarliestDeparture.ToUnixTime();
            _lastArrival = settings.LastArrival.ToUnixTime();
            _tdb = settings.TransitDb;
            _connectionsProvider = _tdb.ConnectionsDb;

            _stopsDb = _tdb.StopsDb;
            _stopsReader = _stopsDb.GetReader();

            _transferPolicy = settings.TransferPolicy;
            _walkPolicy = settings.WalkPolicy;

            _userTargetLocations = settings.TargetStop;

            foreach (var (loc, j) in settings.DepartureStop)
            {
                var journey = j
                              ?? new Journey<T>(loc, settings.EarliestDeparture.ToUnixTime(), settings.StatsFactory);

                _s.Add(loc, journey);
            }
        }


        /// <summary>
        /// Calculates the journey that arrives as early as possible, as specified by the constructor parameters.
        /// Returns null if no journey could be found;
        ///
        /// Note that running this will, as a side effect, also calculate a profile of what location can be reached with an earliest arrival time.
        /// This can be used to optimize PCS later on.
        /// This profile will have scanned (and thus be reliable) up till the latest scanned departure time.
        ///
        /// In other words, it is important to know when the latest departed connection has left.
        /// - In the case that no route is found, the algorithm will stop with simulating departures after 'lastDeparture' as specified in the ctor
        /// - If a route is found, no departures after the earliest arrival are still calculated, unless...
        /// - ... unless a function 'depArrivalToTimeout' is given. Then, that function can calculate the latest can departure time. This will only be run once the earliest arrival has converged
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Journey<T> CalculateJourney(Func<Time, Time, Time> depArrivalToTimeout = null)
        {
            var enumerator = _connectionsProvider.GetDepartureEnumerator();
            enumerator.MoveNext(ScanBeginTime);

            var lastDeparture = _lastArrival;
            Journey<T> bestJourney = null;
            while (enumerator.DepartureTime <= lastDeparture)
            {
                if (!IntegrateBatch(enumerator))
                {
                    // Only happens if database is exhausted
                    break;
                }

                // we have reached a new batch of departure times
                // Let's first check if we can reach an end destination already

                /*
                 * if(GetBestTime().bestTime != uint.MaxValue){
                 *  -> We found a best route, with a best arrival time.
                 *  -> We lower the 'scan until'-time (lastDeparture) to the time we have found
                 *  lastDeparture = GetBestTime() < lastDeparture ? GetBestTime().bestTime : lastDeparture
                 *
                 * if(GetBestTime().bestTime == uint.MaxValue)
                 *  -> No best route is found yet
                 *  -> we do not update lastDeparture.
                 *
                 * The above pseudo code is summarized with:
                 */
                bestJourney = GetBestJourney();
                lastDeparture = Math.Min(bestJourney.Time, lastDeparture);
            }

            // If we en up here, normally we should have found a route.
            bestJourney = bestJourney ?? GetBestJourney();
            if (bestJourney.Time == Time.MaxValue)
            {
                // Sadly, we didn't find a route within the required time
                return null;
            }

            if (depArrivalToTimeout == null)
            {
                // We do not need to extend the search
                ScanEndTime = lastDeparture;
                return bestJourney;
            }

            // Wait! There is one more thing!
            // The user might need a profile to optimize PCS later on
            // We got an alternative end time, we still calculate a little
            ScanEndTime = depArrivalToTimeout(bestJourney.Root.Time, bestJourney.Time);
            while (enumerator.DepartureTime < ScanEndTime)
            {
                if (!IntegrateBatch(enumerator))
                {
                    break;
                }
            }

            return bestJourney;
        }

        /// <summary>
        /// Integrates all connections which happen to have the same departure time.
        /// Once all those connections are handled, the walks from the improved locations are batched
        /// </summary>
        /// <param name="enumerator"></param>
        private bool IntegrateBatch(ConnectionsDb.DepartureEnumerator enumerator)
        {
            var improvedLocations = new HashSet<(uint, uint)>();
            var lastDepartureTime = enumerator.DepartureTime;
            do
            {
                if (IntegrateConnection(enumerator))
                {
                    improvedLocations.Add(enumerator.ArrivalStop);
                }

                if (!enumerator.MoveNext())
                {
                    return false;
                }
            } while (lastDepartureTime == enumerator.DepartureTime);


            // Add footpath transfers to improved stations
            foreach (var location in improvedLocations)
            {
                WalkAwayFrom(location);
            }

            return true;
        }


        /// <summary>
        /// Handle a single connection, update the stop positions with new times if possible.
        ///
        /// Returns connection.ArrivalLocation iff this an improvement has been made to reach this location.
        /// If not, MaxValue is returned
        ///
        /// Returns true if an improvement to c.ArrivalLocation has been made
        /// 
        /// </summary>
        /// <param name="c">A DepartureEnumeration, which is used here as if it were a single connection object</param>
        private bool IntegrateConnection(
            IConnection c)
        {
            // The connection describes a random connection somewhere
            // Lets check if we can take it

            var journeyTillDeparture = GetJourneyTo(c.DepartureStop);


            var trip = c.TripId;
            if (c.DepartureTime < journeyTillDeparture.Time && !_trips.ContainsKey(trip))
            {
                // This connection has already left before we can make it to the stop
                return false;
            }


            Journey<T> journeyToArrival = null;
            // Extend trip journey: if we already are on the trip we can always stay seated on it
            if (_trips.ContainsKey(trip))
            {
                _trips[trip] = _trips[trip].ChainForward(c);
                journeyToArrival = _trips[trip];
            }
            else
            {
                if (journeyTillDeparture.SpecialConnection && journeyTillDeparture.Connection == Journey<T>.GENESIS)
                {
                    journeyToArrival = journeyTillDeparture.ChainForward(c);
                }
                else
                {
                    journeyToArrival =
                        _transferPolicy
                            .CreateDepartureTransfer(_tdb, journeyTillDeparture, c.DepartureTime, c.DepartureStop)
                            ?.ChainForward(c);
                }

                if (journeyToArrival != null && c.CanGetOn())
                {
                    // If we can get on the connection, we keep track of the trip
                    // We don't necessarily have to get off at c.ArrivalStop, so we don't have to check 'canGetOff'
                    _trips[trip] = journeyToArrival;
                }
            }

            if (journeyToArrival == null)
            {
                return false;
            }


            // Update the possible journeys index
            // But for that, we should be able to get of at the arrival stop!
            if (!c.CanGetOff())
            {
                return false;
            }

            if (!_s.ContainsKey(c.ArrivalStop))
            {
                _s[c.ArrivalStop] = journeyToArrival;
                return true;
            }

            var oldJourney = _s[c.ArrivalStop];
            if (journeyToArrival.Time >= oldJourney.Time)
            {
                return false;
            }

            if (c.CanGetOff())
            {
                _s[c.ArrivalStop] = journeyToArrival;
            }

            return true;
        }


        private void WalkAwayFrom((uint, uint) location)
        {
            if (_walkPolicy == null || _walkPolicy.Range() <= 0f)
            {
                return;
            }

            _stopsReader.MoveTo(location);
            var reachableLocations =
                _stopsDb.LocationsInRange(_stopsReader, _walkPolicy.Range());

            var journey = _s[location];

            foreach (var reachableLocation in reachableLocations)
            {
                var id = reachableLocation.Id;
                if (id == location)
                {
                    continue;
                }

                var walkingJourney = _walkPolicy.CreateDepartureTransfer(_tdb, journey, ulong.MaxValue, id);
                if (walkingJourney == null)
                {
                    continue;
                }

                if (!_s.ContainsKey(id))
                {
                    _s[id] = walkingJourney;
                }
                else if (_s[id].Time > walkingJourney.Time)
                {
                    _s[id] = walkingJourney;
                }
            }
        }


        /// <summary>
        /// Searches the best performing journey amongst the target locations.
        /// Note that the target locations could be amended with a 'trailing' journey (e.g. a walk from a stop to the actual target)
        /// </summary>
        /// <returns></returns>
        private Journey<T> GetBestJourney()
        {
            var currentBestJourney = Journey<T>.InfiniteJourney;
            foreach (var (targetLoc, restingJourney) in _userTargetLocations)
            {
                if (!_s.ContainsKey(targetLoc))
                {
                    continue;
                }

                // The journey to 'targetLoc' according to the algorithm + the resting journey to 'go home'
                var journey = _s[targetLoc].Append(restingJourney);

                if (journey.Time > _lastArrival)
                {
                    // We skip this connection: it arrives too late
                    // Either we hope another connection does arrive in time

                    // Note that EAS is monotone: if a good solution is found, we won't search further
                    continue;
                }

                if (journey.Time < currentBestJourney.Time)
                {
                    currentBestJourney = journey;
                }
            }

            return currentBestJourney;
        }


        private Journey<T>
            GetJourneyTo((uint localTileId, uint localId) stop)
        {
            return _s.ContainsKey(stop)
                ? _s[stop]
                : Journey<T>.InfiniteJourney;
        }
    }
}