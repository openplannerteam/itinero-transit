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

        public EarliestConnectionScan(IEnumerable<LocId> userDepartureLocation,
            List<LocId> userTargetLocation, Time earliestDeparture, Time lastDeparture,
            ConnectionsDb connectionsProvider, StopsDb locationsProvider, T statsFactory)
        {
            _locationsProvider = locationsProvider;
            _lastDeparture = lastDeparture;
            _connectionsProvider = connectionsProvider;

            _userTargetLocation = userTargetLocation;
            foreach (var loc in userDepartureLocation)
            {
                _s.Add(loc, new Journey<T>(loc, earliestDeparture, statsFactory));
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
                var lastDepartureTime = enumerator.DepartureTime;
                do
                {
                    IntegrateConnection(enumerator);

                    enumerator.MoveNext(
                        "Could not calculate AES: enumerator depleted, the query is probably to far in the future or the database isn't loaded sufficiently");
                } while (lastDepartureTime == enumerator.DepartureTime);

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
                IntegrateConnection(enumerator);

                enumerator.MoveNext(
                    "Could not finish AES profile: enumerator depleted, the query is probably to far in the future or the database isn't loaded sufficiently");
            }

            return journey;
        }


        /// <summary>
        /// Handle a single connection, update the stop positions with new times if possible
        /// </summary>
        private void IntegrateConnection(ConnectionsDb.DepartureEnumerator c)
        {
            /// <param name="c">A DepartureEnumeration, which is used here as if it were a single connection object</param>
            // The connection describes a random connection somewhere
            // Lets check if we can take it

            var journeyTillDeparture = GetJourneyTo(c.DepartureLocation());

            if (journeyTillDeparture
                .Equals(Journey<T>.InfiniteJourney))
            {
                // The stop where this connection starts, is not yet reachable
                // Abort
                return;
            }


            if (c.DepartureTime < journeyTillDeparture.Time)
            {
                // This connection has already left before we can make it to the stop
                return;
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
                t2 = _trips[trip] = _trips[trip].Chain(c.CurrentId, c.ArrivalTime(), c.ArrivalLocation());
            }
            else
            {
                // We now for sure know that we can board this connection, and thus this trip
                // This is the first encounter of it.
                // The departure station should be stable in time, so we can take that journey and board


                t2 = _trips[trip] = journeyTillDeparture.Transfer
                    (c.CurrentId, c.DepartureTime, c.ArrivalTime(), c.ArrivalLocation());
            }
            
            
            

            if (journeyTillDeparture.LastTripId(_connectionsProvider) != null
                && !Equals(journeyTillDeparture
                    .LastTripId(_connectionsProvider), c.TripId))
            {
                // We have to transfer vehicles
                t1 = journeyTillDeparture.Transfer(
                    c.CurrentId, c.DepartureTime, c.ArrivalTime(), c.ArrivalLocation());
            }
            else
            {
                // This connection was a walk or something similar
                // Or we didn't have to transfer
                // We chain the current connection after it
                t1 = journeyTillDeparture.Chain(c.CurrentId, c.ArrivalTime(), c.ArrivalLocation());
            }

            var journeyTillArrival = GetJourneyTo(c.ArrivalLocation());

            
            
            // Jej! We can take the train! 
            // Lets see if we can make an improvement in regards to the previous solution
           var newJourney = SelectEarliest(journeyTillArrival, t1, t2);
            _s[c.ArrivalLocation()] = newJourney;

            
            var improved = newJourney != journeyTillArrival;
            // if we made an improvement, we should mark all locations that can be reached via foothpaths as reachable
            // TODO


        } /*
            if (c is IContinuousConnection)
            {
                return;
            }

            // Alright, we are arriving at a new location. This means that we can walk from this location onto new stops
            var connections = _profile.WalkToCloseByStops(c.ArrivalTime(),
                _profile.GetCoordinateFor(c.ArrivalLocation()),
                _profile.IntermodalStopSearchRadius);

            foreach (var walk in connections)
            {
                if (walk == null)
                {
                    continue;
                }

                IntegrateConnection(walk);
            }
        }
*/

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