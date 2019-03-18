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
    /// Calculates the fastest journey from A to B arriving at a given time; using CSA (backward A*).
    /// It does _not_ use footpath interlinks (yet)
    /// </summary>
    internal class LatestConnectionScan<T>
        where T : IJourneyStats<T>
    {
        private readonly List<((uint localTileId, uint localId), Journey<T>)> _userDepartureLocation;

        private readonly TransitDb.TransitDbSnapShot _tdb;
        private readonly ConnectionsDb _connectionsProvider;

        private readonly Time _earliestDeparture;

        private readonly IOtherModeGenerator _transferPolicy;

        public ulong ScanBeginTime { get; private set; } = Time.MaxValue;

        public ulong ScanEndTime { get; }

        public IReadOnlyDictionary<(uint localTileId, uint localId), Journey<T>> Isochrone() => _s;


        /// <summary>
        /// This dictionary keeps, for each stop, the journey that arrives as late as possible
        /// </summary>
        private readonly Dictionary<(uint localTileId, uint localId), Journey<T>> _s =
            new Dictionary<(uint localTileId, uint localId), Journey<T>>();


        /// <summary>
        /// Keeps track of where we are on each trip, thus if we wouldn't leave a bus once we're on it
        /// </summary>
        private readonly Dictionary<uint, Journey<T>> _trips = new Dictionary<uint, Journey<T>>();


        public LatestConnectionScan(ScanSettings<T> settings)
        {
            settings.SanityCheck();
            _tdb = settings.TransitDb;
            _earliestDeparture = settings.EarliestDeparture.ToUnixTime();
            ScanEndTime = settings.LastArrival.ToUnixTime();
            _connectionsProvider = _tdb.ConnectionsDb;
            _transferPolicy = settings.TransferPolicy;
            _userDepartureLocation = settings.DepartureStop;
            foreach (var (loc, j) in settings.TargetStop)
            {
                var journey = j?.SetTag(Journey<T>.LatestArrivalScanJourney)
                              ?? new Journey<T>(loc, settings.LastArrival.ToUnixTime(),
                                  settings.StatsFactory,
                                  Journey<T>.LatestArrivalScanJourney);
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
            enumerator.MovePrevious(ScanEndTime);

            var earliestAllowedDeparture = _earliestDeparture;
            Journey<T> bestJourney = null;
            while (enumerator.DepartureTime >= earliestAllowedDeparture)
            {
                if (!IntegrateBatch(enumerator))
                {
                    break;
                }

                // we have reached a new batch of departure times
                // Let's first check if we can reach an end destination already

                /*
                 * if(GetBestTime().bestTime != uint.MinValue){
                 *  -> We found a best route, with a best departure time.
                 *  -> We heighten the 'scan until'-time (earliestAllowedDeparture) to the time we have found
                 *
                 * if(GetBestTime().bestTime == uint.MinValue)
                 *  -> No best route is found yet
                 *  -> we do not update earliestAllowedDeparture.
                 *
                 * The above pseudo code is summarized with:
                 */
                bestJourney = GetBestJourney();
                earliestAllowedDeparture = Math.Max(bestJourney.Time, _earliestDeparture);
            }

            // If we en up here, normally we should have found a route.

            bestJourney = bestJourney ?? GetBestJourney();
            if (bestJourney.Time == Time.MinValue)
            {
                // Sadly, we didn't find a route within the required time
                return null;
            }

            bestJourney = bestJourney.Reversed()[0];
            if (depArrivalToTimeout == null)
            {
                ScanBeginTime = bestJourney.Root.Time;
                return bestJourney;
            }

            // Wait! There is one more thing!
            // The user might need a profile to optimize PCS later on
            // We got an alternative end time, we still calculate a little
            ScanBeginTime = depArrivalToTimeout(bestJourney.Root.Time, bestJourney.Time);
            while (enumerator.DepartureTime >= ScanBeginTime)
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
            var lastDepartureTime = enumerator.DepartureTime;
            do
            {
                IntegrateConnection(enumerator);

                if (!enumerator.MovePrevious())
                {
                    return false;
                }
            } while (lastDepartureTime == enumerator.DepartureTime);

            return true;
        }


        /// <summary>
        /// Handle a single connection, update the stop positions with new times if possible.
        ///
        /// Returns connection.ArrivalLocation iff this an improvement has been made to reach this location.
        /// If not, MinValue is returned
        /// 
        /// </summary>
        /// <param name="c">A DepartureEnumeration, which is used here as if it were a single connection object</param>
        private void IntegrateConnection(
            IConnection c)
        {
            // The connection describes a random connection somewhere
            // Lets check if we can take it

            var journeyFromArrival = GetJourneyFrom(c.ArrivalStop);


            var trip = c.TripId;
            if (c.ArrivalTime > journeyFromArrival.Time && !_trips.ContainsKey(trip))
            {
                // This connection has already left before we can make it to the stop
                return;
            }


            Journey<T> journeyFromDeparture;
            // Extend trip journey
            if (_trips.ContainsKey(trip))
            {
                _trips[trip] = _trips[trip].ChainBackward(c);
                journeyFromDeparture = _trips[trip];
            }
            else
            {
                if (journeyFromArrival.SpecialConnection && journeyFromArrival.Connection == Journey<T>.GENESIS)
                {
                    journeyFromDeparture = journeyFromArrival.ChainBackward(c);
                }
                else
                {
                    journeyFromDeparture = _transferPolicy
                        .CreateArrivingTransfer(_tdb, journeyFromArrival, c.ArrivalTime, c.ArrivalStop)
                        ?.ChainBackward(c);
                }

                if (journeyFromDeparture != null && c.CanGetOn())
                {
                    _trips[trip] = journeyFromDeparture;
                }
            }

            if (journeyFromDeparture == null)
            {
                return;
            }

            // Below this point, we only add it to the journey table...
            // If we can get off at the arrivalStop that is

            if (!c.CanGetOff())
            {
                return;
            }

            if (!_s.ContainsKey(c.DepartureStop))
            {
                _s[c.DepartureStop] = journeyFromDeparture;
            }
            else
            {
                var oldJourney = _s[c.DepartureStop];

                if (journeyFromDeparture.Time > oldJourney.Time)
                {
                    _s[c.DepartureStop] = journeyFromDeparture;
                }
            }
        }

        /// <summary>
        /// Iterates all the target locations.
        /// Returns the earliest time that one of them can be reached, along with the chosen location.
        /// If no location can be reached, returns 'Time.MinValue'
        /// </summary>
        /// <returns></returns>
        private Journey<T> GetBestJourney()
        {
            var currentBestJourney = Journey<T>.NegativeInfiniteJourney;
            foreach (var (targetLoc, restingJourney) in _userDepartureLocation)
            {
                if (!_s.ContainsKey(targetLoc))
                {
                    continue;
                }

                var journey = _s[targetLoc].Append(restingJourney);

                if (journey.Time < _earliestDeparture)
                {
                    // Journey departs to early, we skip it
                    continue;
                }

                if (journey.Time > currentBestJourney.Time)
                {
                    currentBestJourney = journey;
                }
            }

            return currentBestJourney;
        }


        private Journey<T>
            GetJourneyFrom((uint, uint) location)
        {
            return _s.ContainsKey(location)
                ? _s[location]
                : Journey<T>.NegativeInfiniteJourney;
        }
    }
}