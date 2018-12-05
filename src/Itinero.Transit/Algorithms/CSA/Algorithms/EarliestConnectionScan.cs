using System;
using System.Collections.Generic;
using Itinero.Transit.Data;

namespace Itinero.Transit
{
    using LocId = UInt64;
    using Time = UInt64;

    /// <summary>
    /// Calculates the fastest journey from A to B starting at a given time; using CSA (forward A*).
    /// It will download only the linked connections it needs.
    /// It does _not_ use footpath interlinks (yet)
    /// </summary>
    public class EarliestConnectionScan<T>
        where T : IJourneyStats<T>
    {
        private readonly Profile<T> _profile;

        private readonly List<LocId> _userTargetLocation;

        private readonly ConnectionsDb _connectionsProvider;
        private readonly StopsDb _locationsProvider;

        private readonly Time _lastDeparture;

        /// <summary>
        /// This dictionary keeps, for each stop, the journey that arrives as early as possible
        /// </summary>
        private readonly Dictionary<LocId, Journey<T>> _s = new Dictionary<LocId, Journey<T>>();

        /// <summary>
        /// Keeps track of where we are on each trip, thus if we wouldn't leave a bus once we're on it
        /// </summary>
        private readonly Dictionary<LocId, Journey<T>> _trips = new Dictionary<LocId, Journey<T>>();


        /// <summary>
        /// This dictionary maps arrival times on the respective locations (when the traveller could arriva at that location)
        /// When the algorithm comes at that arrival time, the walks from that arrival are calculated
        /// </summary>
        //   private readonly Dictionary<Time, HashSet<LocId>> _knownDepartures = new Dictionary<uint, HashSet<ulong>>();
        
        
        /// <summary>
        /// Construct a AES
        /// </summary>
        /// <param name="userDepartureLocation"></param>
        /// <param name="userTargetLocation"></param>
        /// <param name="earliestDeparture"></param>
        /// <param name="lastDeparture"></param>
        /// <param name="profile"></param>
        public EarliestConnectionScan(LocId userDepartureLocation, LocId userTargetLocation,
            Time earliestDeparture, Time lastDeparture,
            Profile<T> profile) : this(
            new List<ulong> {userDepartureLocation}, new List<ulong> {userTargetLocation},
            earliestDeparture, lastDeparture,
            profile)
        {
        }


        public EarliestConnectionScan(IEnumerable<LocId> userDepartureLocation,
            List<LocId> userTargetLocation, Time earliestDeparture, Time lastDeparture,
            Profile<T> profile)
        {
            _profile = profile;
            _locationsProvider = profile.StopsDb;
            _lastDeparture = lastDeparture;
            _connectionsProvider = profile.ConnectionsDb;

            _userTargetLocation = userTargetLocation;
            foreach (var loc in userDepartureLocation)
            {
                _s.Add(loc, new Journey<T>(loc, earliestDeparture, profile.StatsFactory));
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
            // Calculate when we will start the journey
            Time? startTime = null;
            // A few locations will already have a start location
            foreach (var k in _s.Keys)
            {
                var j = _s[k];
                var t = j.StartTime();
                if (startTime == null)
                {
                    startTime = t;
                }
                else if (t < startTime)
                {
                    startTime = t;
                }
            }

            var start = startTime ?? throw new ArgumentException("Can not EAS without a start journey ");


            ConnectionsDb.DepartureEnumerator enumerator = _connectionsProvider.GetDepartureEnumerator();

            // Move the enumerator to the start time
            while (enumerator.DepartureTime < start)
            {
                enumerator.MoveNext(
                    "Could not calculate AES: departure time not found. Either to little connections are loaded in the database, or the query is to far in the future or in the past");
            }


            var lastDeparture = _lastDeparture;
            while (enumerator.DepartureTime < lastDeparture)
            {
                IntegrateBatch(enumerator);

                // we have reached a new batch of departure times
                // Let's first check if we can reach an end destination already

                lastDeparture = Math.Min(GetBestTime().bestTime, lastDeparture);
            }

            // If we en up here, normally we should have found a route.

            var route = GetBestTime();
            if (route.bestLocation == null)
            {
                // Sadly, we didn't find a route within the required time
                return null;
            }


            // We grab the journey we need
            var journey = _s[(uint) route.bestLocation];

            if (depArrivalToTimeout == null)
            {
                return journey;
            }

            // Wait! There is one more thing!
            // The user might need a profile to optimize PCS later on
            // We got an alternative end time, we still calculate a little
            lastDeparture = depArrivalToTimeout(journey.Root.Time, journey.Time);
            while (enumerator.DepartureTime < lastDeparture)
            {
                IntegrateBatch(enumerator);
            }


            return journey;
        }

        /// <summary>
        /// Integrates all connections which happen to have the same departure time.
        /// Once all those connections are handled, the walks from the improved locations are batched
        /// </summary>
        /// <param name="enumerator"></param>
        private void IntegrateBatch(ConnectionsDb.DepartureEnumerator enumerator)
        {
            var lastDepartureTime = enumerator.DepartureTime;
            do
            {
                var l = IntegrateConnection(enumerator);

                /*   if (l.improvedLocation != LocId.MaxValue)
                   {
                       // The location has improved - we add it to the _knownDepartures
                       
                       // First, remove it from the previous departure time set
                       if (!_knownDepartures.ContainsKey(l.previousTime))
                       {
                           _knownDepartures[l.previousTime].Remove(l.improvedLocation);
                       }
   
                       if (!_knownDepartures.ContainsKey(enumerator.ArrivalTime))
                       {
                           _knownDepartures[enumerator.ArrivalTime] = new HashSet<ulong>();
                       }
   
                       // We add it to the correct bucket
                       _knownDepartures[enumerator.ArrivalTime].Add(l.improvedLocation);
                   }
                   */
                enumerator.MoveNext(
                    "Could not calculate Earliest Connection: enumerator depleted, the query is probably to far in the future or the database isn't loaded sufficiently");
            } while (lastDepartureTime == enumerator.DepartureTime);

            // The timeblock 
        }


        /// <summary>
        /// Handle a single connection, update the stop positions with new times if possible.
        ///
        /// Returns connection.ArrivalLocation iff this an improvement has been made to reach this location.
        /// If not, MaxValue is returned
        /// 
        /// </summary>
        private (LocId improvedLocation, Time previousTime) IntegrateConnection(Connection c)
        {
            /// <param name="c">A DepartureEnumeration, which is used here as if it were a single connection object</param>
            // The connection describes a random connection somewhere
            // Lets check if we can take it

            var journeyTillDeparture = GetJourneyTo(c.DepartureLocation);

            if (journeyTillDeparture
                .Equals(Journey<T>.InfiniteJourney))
            {
                // The stop where this connection starts, is not yet reachable
                // Abort
                return (LocId.MaxValue, Time.MaxValue);
            }


            if (c.DepartureTime < journeyTillDeparture.Time)
            {
                // This connection has already left before we can make it to the stop
                return (LocId.MaxValue, Time.MaxValue);
            }


            // When transferring
            var t1 = Journey<T>.InfiniteJourney;

            // When resting in a trip
            var t2 = Journey<T>.InfiniteJourney;

            // When walking
            // Walks are handled downwards

            var trip = c.TripId;

            if (_trips.ContainsKey(trip))
            {
                // We could be on this trip already, lets extend the journey
                t2 = _trips[trip] = _trips[trip].ChainForward(c);
            }
            else
            {
                // We now for sure know that we can board this connection, and thus this trip
                // This is the first encounter of it.
                // The departure station should be stable in time, so we can take that journey and board


                t2 = _trips[trip] =
                    // TODO
                    journeyTillDeparture.Transfer
                        (c.Id, c.DepartureTime, c.ArrivalTime, c.ArrivalLocation, c.TripId);
            }


            if (journeyTillDeparture.LastTripId() != null
                && !Equals(journeyTillDeparture
                    .LastTripId(), c.TripId))
            {
                // We have to transfer vehicles
                t1 = journeyTillDeparture.Transfer(
                    c.Id, c.DepartureTime, c.ArrivalTime, c.ArrivalLocation, c.TripId);
            }
            else
            {
                // This connection was a walk or something similar
                // Or we didn't have to transfer
                // We chain the current connection after it
                t1 = journeyTillDeparture.ChainForward(c);
            }

            var journeyTillArrival = GetJourneyTo(c.ArrivalLocation);


            // Jej! We can take the train! 
            // Lets see if we can make an improvement in regards to the previous solution
            var newJourney = SelectEarliest(journeyTillArrival, t1, t2);
            _s[c.ArrivalLocation] = newJourney;


            var improved = newJourney != journeyTillArrival;
            return (improved ? c.ArrivalLocation : LocId.MaxValue, journeyTillArrival.Time);
        }

        /// <summary>
        /// Iterates all the target locations.
        /// Returns the earliest time that one of them can be reached, along with the chosen location.
        /// If no location can be reached, returns 'Time.MaxValue'
        /// </summary>
        /// <returns></returns>
        private (Time bestTime, LocId? bestLocation) GetBestTime()
        {
            var currentBestArrival = Time.MaxValue;
            LocId? bestTarget = null;
            foreach (var targetLoc in _userTargetLocation)
            {
                var arrival = GetJourneyTo(targetLoc).Time;

                if (arrival < currentBestArrival)
                {
                    currentBestArrival = arrival;
                    bestTarget = targetLoc;
                }
            }

            return (currentBestArrival, bestTarget);
        }

        private Journey<T> SelectEarliest(params Journey<T>[] journeys)
        {
            var earliest = journeys[0];
            var earliestTime = earliest.Time;
            foreach (var journey in journeys)
            {
                var arrTime = journey.Time;
                if (arrTime < earliestTime)
                {
                    earliest = journey;
                    earliestTime = arrTime;
                }
            }

            return earliest;
        }

        private Journey<T> GetJourneyTo(LocId stop)
        {
            return
                _s.ContainsKey(stop)
                    ? _s[stop]
                    : Journey<T>.InfiniteJourney;
        }
    }
}