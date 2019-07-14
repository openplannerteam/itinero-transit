using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Transit.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Filter;
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
        private readonly List<(StopId, Journey<T>)> _userTargetLocations;

        private readonly IConnectionEnumerator _connectionsEnumerator;
        private readonly IStopsReader _stopsReader;

        /// <summary>
        /// The last allowed departure time. Note that scanning could continue after it, if a scan-overshoot is given
        /// </summary>
        private readonly ulong _lastArrival;

        internal IReadOnlyDictionary<StopId, Journey<T>> Isochrone() => JourneyFromDepartureTable;

        public ulong ScanEndTime { get; private set; } = ulong.MinValue;

        public ulong ScanBeginTime { get; }

        private readonly IOtherModeGenerator _transferPolicy;
        private readonly IOtherModeGenerator _walkPolicy;

        /// <summary>
        /// If a traveller has a hard preference on journeys (e.g. max 5 transfers, no specific combination of stations...),
        /// this can be expressed with the journeyFilter 
        /// </summary>
        private readonly IJourneyFilter<T> _journeyFilter;

        private readonly IConnectionFilter _connectionFilter;

        /// <summary>
        /// This dictionary keeps, for each stop, the journey that arrives as early as possible
        /// </summary>
        internal readonly Dictionary<StopId, Journey<T>> JourneyFromDepartureTable =
            new Dictionary<StopId, Journey<T>>();

        /// <summary>
        /// Keeps track of where we are on each trip, thus if we wouldn't leave a bus once we're on it
        /// </summary>
        private readonly Dictionary<TripId, Journey<T>> _trips = new Dictionary<TripId, Journey<T>>();


        public EarliestConnectionScan(
            ScanSettings<T> settings)
        {
            ScanBeginTime = settings.EarliestDeparture.ToUnixTime();
            _lastArrival = settings.LastArrival.ToUnixTime();
            _connectionsEnumerator = settings.ConnectionsEnumerator;
            _stopsReader = settings.StopsReader;

            _transferPolicy = settings.Profile.InternalTransferGenerator;

            _userTargetLocations = settings.TargetStop;
            _journeyFilter = settings.Profile.JourneyFilter;
            _connectionFilter = settings.Profile.ConnectionFilter; // settings.Filter is NOT used and SHOULD NOT BE used
            _walkPolicy = settings.Profile.WalksGenerator;


            foreach (var (loc, j) in settings.DepartureStop)
            {
                var journey = j
                              ?? new Journey<T>(
                                  loc, settings.EarliestDeparture.ToUnixTime(), settings.Profile.MetricFactory,
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
            var enumerator = _connectionsEnumerator;
            var currentConnection = new Connection();

            enumerator.MoveTo(ScanBeginTime);
            if (!enumerator.HasNext())
            {
                throw new Exception("Empty enumerator, can not calculate EAS");
            }

            var lastDeparture = _lastArrival;
            Journey<T> bestJourney = null;
            while (currentConnection.DepartureTime <= lastDeparture)
            {
                if (!IntegrateBatch())
                {
                    // Only happens if database is exhausted
                    bestJourney = null;
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
            while (enumerator.CurrentDateTime < ScanEndTime)
            {
                if (!IntegrateBatch())
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
        private bool IntegrateBatch()
        {
            var improvedLocations = new HashSet<StopId>();
            var lastDepartureTime = _connectionsEnumerator.CurrentDateTime;
            bool hasNext;
            var c = new Connection();
            do
            {
                
                // The enumerator should already be initialized on the next entry
                _connectionsEnumerator.Current(c);
                if (IntegrateConnection(c))
                {
                    improvedLocations.Add(c.ArrivalStop);
                }

                hasNext = _connectionsEnumerator.HasNext();
            } while (hasNext && _connectionsEnumerator.CurrentDateTime == lastDepartureTime);


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
            Connection c)
        {
            // The connection describes a random connection somewhere
            // Lets check if we can take it

            if (_connectionFilter != null
                && !_connectionFilter.CanBeTaken(c)
            )
            {
                // Filtered away...
                return false;
            }


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
                journeyToArrival = _trips[trip].ChainForward(c);
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
                    // The total time needed to transfer
                    var timeNeeded =
                        _transferPolicy.TimeBetween(_stopsReader, journeyTillDeparture.Location, c.DepartureStop);

                    if (journeyTillDeparture.Time + timeNeeded <= c.DepartureTime)
                    {
                        journeyToArrival =
                            journeyTillDeparture
                                .ChainForwardWith(_stopsReader, _transferPolicy, c.DepartureStop)
                                ?.ChainForward(c);
                    }
                    else
                    {
                        journeyToArrival = null;
                    }
                }
            }

            if (journeyToArrival == null)
            {
                // There was no way to be on the trip: neither by staying seated or by getting on
                return false;
            }


            if (_journeyFilter != null && !_journeyFilter.CanBeTaken(journeyToArrival))
            {
                // The traveller doesn't want to take this journey for some reason or another
                return false;
            }

            // We register that we can be on this trip: we could get on (or we already were on this trip)
            _trips[trip] = journeyToArrival;

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
        /// <exception cref="ArgumentException"></exception>
        private void WalkAwayFrom(StopId location)
        {
            if (_walkPolicy == null || _walkPolicy.Range() <= 0f)
            {
                return;
            }

            var journey = JourneyFromDepartureTable[location];

            foreach (var walkingJourney in journey.WalkAwayFrom(_walkPolicy, _stopsReader))
            {
                var id = walkingJourney.Location;

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
            GetJourneyTo(StopId stop)
        {
            return JourneyFromDepartureTable.ContainsKey(stop)
                ? JourneyFromDepartureTable[stop]
                : Journey<T>.InfiniteJourney;
        }
    }
}