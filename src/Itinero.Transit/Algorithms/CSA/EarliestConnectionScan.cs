using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Transit.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Utils;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]

namespace Itinero.Transit.Algorithms.CSA
{

    /// <summary>
    /// Calculates the fastest journey from A to B starting at a given time; using CSA (forward A*).
    /// It does _not_ use footpath interlinks (yet)
    /// </summary>
    internal class EarliestConnectionScan<T>
        where T : IJourneyMetric<T>
    {
        private readonly List<(LocationId, Journey<T>)> _userTargetLocations;

        private readonly IConnectionEnumerator _connections;
        private readonly IStopsReader _stopsReader;

        /// <summary>
        /// The last allowed departure time. Note that scanning could continue after it, if a scan-overshoot is given
        /// </summary>
        private readonly ulong _lastArrival;

        internal IReadOnlyDictionary<LocationId, Journey<T>> Isochrone() => JourneyFromDepartureTable;

        public ulong ScanEndTime { get; private set; } = ulong.MinValue;

        public ulong ScanBeginTime { get; }

        private readonly IOtherModeGenerator _transferPolicy, _walkPolicy;

        /// <summary>
        /// This dictionary keeps, for each stop, the journey that arrives as early as possible
        /// </summary>
        internal readonly Dictionary<LocationId, Journey<T>> JourneyFromDepartureTable =
            new Dictionary<LocationId, Journey<T>>();

        /// <summary>
        /// Keeps track of where we are on each trip, thus if we wouldn't leave a bus once we're on it
        /// </summary>
        private readonly Dictionary<TripId, Journey<T>> _trips = new Dictionary<TripId, Journey<T>>();

        public EarliestConnectionScan(
            ScanSettings<T> settings)
        {
            ScanBeginTime = settings.EarliestDeparture.ToUnixTime();
            _lastArrival = settings.LastArrival.ToUnixTime();
            _connections = settings.ConnectionsEnumerator;
            _stopsReader = settings.StopsReader;

            _transferPolicy = settings.TransferPolicy;
            _walkPolicy = settings.WalkPolicy;

            _userTargetLocations = settings.TargetStop;

            foreach (var (loc, j) in settings.DepartureStop)
            {
                var journey = j
                              ?? new Journey<T>(
                                  loc, settings.EarliestDeparture.ToUnixTime(), settings.MetricFactory,
                                  Journey<T>.EarliestArrivalScanJourney);

                JourneyFromDepartureTable.Add(loc, journey);
                // Walk away from this departure location, to have some more departure locations
                WalkAwayFrom(loc);
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
        public Journey<T> CalculateJourney(Func<ulong, ulong, ulong> depArrivalToTimeout = null)
        {
            var enumerator = _connections;

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
            if (bestJourney.Time == ulong.MaxValue)
            {
                // Sadly, we didn't find a route within the required time
                // This could be intentional, e.g. isochrone searching
                ScanEndTime = lastDeparture;
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
        private bool IntegrateBatch(IConnectionEnumerator enumerator)
        {
            var improvedLocations = new HashSet<LocationId>();
            var lastDepartureTime = enumerator.DepartureTime;
            bool hasNext;
            do
            {
                if (IntegrateConnection(enumerator))
                {
                    improvedLocations.Add(enumerator.ArrivalStop);
                }

                hasNext = enumerator.MoveNext();
            } while (
                hasNext &&
                lastDepartureTime == enumerator.DepartureTime);


            // Add footpath transfers to improved stations
            foreach (var location in improvedLocations)
            {
                WalkAwayFrom(location);
            }

            return hasNext;
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


            Journey<T> journeyToArrival;
            // Extend trip journey: if we already are on the trip we can always stay seated on it
            if (_trips.ContainsKey(trip))
            {
                _trips[trip] = _trips[trip].ChainForward(c);
                journeyToArrival = _trips[trip];
            }
            else if (!c.CanGetOn())
            {
                // We are not on the trip
                // And we can't get on...
                // No use to continue
                return false;
            }
            else
            {
                if (journeyTillDeparture.SpecialConnection)
                {
                    // We only insert a transfer after a 'normal' segment
                    journeyToArrival = journeyTillDeparture.ChainForward(c);
                }
                else
                {
                    journeyToArrival =
                        _transferPolicy
                            .CreateDepartureTransfer(_stopsReader, journeyTillDeparture, c.DepartureTime,
                                c.DepartureStop)
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
                // There was no way to be on the trip: neither by staying seated or by getting on
                return false;
            }


            // Update the possible journeys index
            // But for that, we should be able to get of at the arrival stop!
            if (!c.CanGetOff())
            {
                return false;
            }

            if (!JourneyFromDepartureTable.ContainsKey(c.ArrivalStop))
            {
                JourneyFromDepartureTable[c.ArrivalStop] = journeyToArrival;
                return true;
            }


            var oldJourney = JourneyFromDepartureTable[c.ArrivalStop];
            if (journeyToArrival.Time >= oldJourney.Time)
            {
                // No improvement - do not change anything
                return false;
            }

            JourneyFromDepartureTable[c.ArrivalStop] = journeyToArrival;

            return true;
        }


        /// <summary>
        /// If the traveller arrives at a certain stop which has improved, this method:
        /// - Clones the traveller
        /// - Each of the clones walks towards a close enough stop
        /// - Each of the clones checks if it arrives at his stop earlier then previously possible. If so, this is saved into the journey table `_s`
        ///
        /// This method is very unpure
        /// </summary>
        /// <param name="location"></param>
        /// <exception cref="ArgumentException"></exception>
        private void WalkAwayFrom(LocationId location)
        {
            if (_walkPolicy == null || _walkPolicy.Range() <= 0f)
            {
                return;
            }

            if (!_stopsReader.MoveTo(location))
            {
                throw new ArgumentException($"Location {location} not found, could not move to it");
            }

            var reachableLocations =
                _stopsReader.LocationsInRange(_stopsReader.Latitude, _stopsReader.Longitude, _walkPolicy.Range());

            var journey = JourneyFromDepartureTable[location];

            foreach (var reachableLocation in reachableLocations)
            {
                var id = reachableLocation.Id;
                if (id.Equals(location))
                {
                    continue;
                }

                var walkingJourney = _walkPolicy.CreateDepartureTransfer(_stopsReader, journey, ulong.MaxValue, id);
                if (walkingJourney == null)
                {
                    continue;
                }

                if (!JourneyFromDepartureTable.ContainsKey(id))
                {
                    JourneyFromDepartureTable[id] = walkingJourney;
                }
                else if (JourneyFromDepartureTable[id].Time > walkingJourney.Time)
                {
                    JourneyFromDepartureTable[id] = walkingJourney;
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
                if (!JourneyFromDepartureTable.ContainsKey(targetLoc))
                {
                    continue;
                }

                // The journey to 'targetLoc' according to the algorithm + the resting journey to 'go home'
                var journey = JourneyFromDepartureTable[targetLoc].Append(restingJourney);

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
            GetJourneyTo(LocationId stop)
        {
            return JourneyFromDepartureTable.ContainsKey(stop)
                ? JourneyFromDepartureTable[stop]
                : Journey<T>.InfiniteJourney;
        }
    }
}