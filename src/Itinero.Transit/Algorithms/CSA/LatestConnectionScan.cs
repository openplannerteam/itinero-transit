using System;
using System.Collections.Generic;
using Itinero.IO.LC;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Itinero.Transit.Logging;

namespace Itinero.Transit.Algorithms.CSA
{
    //using LocId = UInt64;
    using Time = UInt64;

    /// <summary>
    /// Calculates the fastest journey from A to B arriving at a given time; using CSA (backward A*).
    /// It does _not_ use footpath interlinks (yet)
    /// </summary>
    public class LatestConnectionScan<T> : IConnectionFilter
        where T : IJourneyStats<T>
    {
        private readonly List<(uint localTileId, uint localId)> _userDepartureLocation;

        private readonly ConnectionsDb _connectionsProvider;

        private readonly Time _earliestDeparture, _lastDeparture;

        /// <summary>
        /// At what time can we start using this as a filter?
        /// </summary>
        private Time _filterStartTime = Time.MaxValue;

        private readonly IOtherModeGenerator _transferPolicy;

        /// <summary>
        /// This dictionary keeps, for each stop, the journey that arrives as early as possible
        /// </summary>
        private readonly Dictionary<(uint localTileId, uint localId), Journey<T>> _s =
            new Dictionary<(uint localTileId, uint localId), Journey<T>>();

        /// <summary>
        /// Keeps track of where we are on each trip, thus if we wouldn't leave a bus once we're on it
        /// </summary>
        private readonly Dictionary<uint, Journey<T>> _trips = new Dictionary<uint, Journey<T>>();


        /// <summary>
        /// Construct a AES
        /// </summary>
        /// <param name="userDepartureLocation"></param>
        /// <param name="userTargetLocation"></param>
        /// <param name="earliestDeparture"></param>
        /// <param name="lastDeparture"></param>
        /// <param name="profile"></param>
        public LatestConnectionScan((uint localTileId, uint localId) userDepartureLocation,
            (uint localTileId, uint localId) userTargetLocation,
            DateTime earliestDeparture, DateTime lastDeparture,
            Profile<T> profile) : this(
            new List<(uint localTileId, uint localId)> {userDepartureLocation},
            new List<(uint localTileId, uint localId)> {userTargetLocation},
            (uint) earliestDeparture.ToUnixTime(), (uint) lastDeparture.ToUnixTime(),
            profile)
        {
        }


        public LatestConnectionScan((uint localTileId, uint localId) userDepartureLocation,
            (uint localTileId, uint localId) userTargetLocation,
            ulong earliestDeparture, ulong lastDeparture,
            Profile<T> profile) : this(
            new List<(uint localTileId, uint localId)> {userDepartureLocation},
            new List<(uint localTileId, uint localId)> {userTargetLocation},
            earliestDeparture, lastDeparture,
            profile)
        {
        }


        public LatestConnectionScan(List<(uint localTileId, uint localId)> userDepartureLocation,
            IEnumerable<(uint localTileId, uint localId)> userTargetLocation, Time earliestDeparture,
            Time lastDeparture,
            Profile<T> profile)
        {
            _earliestDeparture = earliestDeparture;
            _lastDeparture = lastDeparture;
            _connectionsProvider = profile.ConnectionsDb;
            _transferPolicy = profile.WalksGenerator;
            _userDepartureLocation = userDepartureLocation;
            foreach (var loc in userTargetLocation)
            {
                _s.Add(loc, new Journey<T>(loc, lastDeparture, profile.StatsFactory));
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

            enumerator.MoveToPrevious(_lastDeparture);


            var earliestAllowedDeparture = _earliestDeparture;
            while (enumerator.DepartureTime >= earliestAllowedDeparture)
            {
                if (!IntegrateBatch(enumerator))
                {
                    break;
                }

                // we have reached a new batch of departure times
                // Let's first check if we can reach an end destination already

                earliestAllowedDeparture = Math.Max(GetBestTime().bestTime, _earliestDeparture);
            }

            // If we en up here, normally we should have found a route.

            var route = GetBestTime();
            if (route.bestTime == Time.MinValue)
            {
                // Sadly, we didn't find a route within the required time
                return null;
            }


            // We grab the journey we need
            var journey = _s[route.bestLocation.Value].Reversed();


            if (depArrivalToTimeout == null)
            {
                return journey;
            }

            // Wait! There is one more thing!
            // The user might need a profile to optimize PCS later on
            // We got an alternative end time, we still calculate a little
            _filterStartTime = depArrivalToTimeout(journey.Root.Time, journey.Time);
            while (enumerator.DepartureTime >= _filterStartTime)
            {
                if (!IntegrateBatch(enumerator))
                {
                    break;
                }
            }

            return journey;
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
                var l = IntegrateConnection(enumerator);
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
        private ((uint localTileId, uint localId) improvedLocation, Time previousTime) IntegrateConnection(
            IConnection c)
        {
            // The connection describes a random connection somewhere
            // Lets check if we can take it

            if (!_s.ContainsKey(c.ArrivalStop))
            {
                // we can not take this connection, it leads nowhere
                return ((uint.MinValue, uint.MinValue), Time.MinValue);
            }

            var journeyTillArrival = _s[c.ArrivalStop];


            if (c.ArrivalTime > journeyTillArrival.Time)
            {
                // This connection arrives after the departure of the rest of the journey
                return ((uint.MinValue, uint.MinValue), Time.MinValue);
            }


            // When transferring
            Journey<T> t1;
            // Walks are handled downwards
            if (journeyTillArrival.LastTripId() != null
                && !Equals(journeyTillArrival.LastTripId(), c.TripId))
            {
                // We have to transfer vehicles
                t1 = journeyTillArrival.Transfer(
                    c.Id, c.ArrivalTime, c.DepartureTime, c.DepartureStop, c.TripId);
            }
            else
            {
                // This connection was a walk or something similar
                // Or we didn't have to transfer
                // We chain the current connection after it
                t1 = journeyTillArrival.ChainForward(c);
            }
            
            
            
            // When resting in a trip
            Journey<T> t2;
            var trip = c.TripId;
            if (_trips.ContainsKey(trip))
            {
                // We could be on this trip already, lets extend the journey
                t2 = _trips[trip] = _trips[trip].ChainBackward(c);
            }
            else
            {
                // We now know for sure know that we can board this connection, and thus this trip
                // This is the first encounter of it.
                // The departure station should be stable in time, so we can take that journey and board


                t2 = _trips[trip] = t1;
            }

            var journeyFromDeparture = GetJourneyFrom(c.DepartureStop);


            // Jej! We can take the train! 
            // Lets see if we can make an improvement in regards to the previous solution
            var newJourney = SelectLatest(journeyFromDeparture, t1, t2);
            _s[c.DepartureStop] = newJourney;


            var improved = newJourney != journeyFromDeparture;
            return (improved ? c.ArrivalStop : (uint.MinValue, uint.MinValue), journeyFromDeparture.Time);
        }

        /// <summary>
        /// Iterates all the target locations.
        /// Returns the earliest time that one of them can be reached, along with the chosen location.
        /// If no location can be reached, returns 'Time.MinValue'
        /// </summary>
        /// <returns></returns>
        private (Time bestTime, (uint localTileId, uint localId)? bestLocation) GetBestTime()
        {
            var currentBestDeparture = Time.MinValue;
            (uint localTileId, uint localId)? bestTarget = null;
            foreach (var targetLoc in _userDepartureLocation)
            {
                if (!_s.ContainsKey(targetLoc))
                {
                    continue;
                }
                var departure = _s[targetLoc].Time;

                if (departure > currentBestDeparture)
                {
                    currentBestDeparture = departure;
                    bestTarget = targetLoc;
                }
            }

            return (currentBestDeparture, bestTarget);
        }

        private Journey<T> SelectLatest(params Journey<T>[] journeys)
        {
            var earliest = journeys[0];
            var earliestTime = earliest.Time;
            foreach (var journey in journeys)
            {
                if (journey == null)
                {
                    continue;
                }
                
                var arrTime = journey.Time;
                if (arrTime < earliestTime)
                {
                    earliest = journey;
                    earliestTime = arrTime;
                }
            }

            return earliest;
        }

        public void CheckWindow(ulong earliestDepTime, ulong latestArrivalTime)
        {
            if (earliestDepTime > _filterStartTime)
            {
                throw new ArgumentException(
                    "This EAS can not be used as connection filter, the requesting algorithm needs connections before my scantime ");
            }


            if (latestArrivalTime < _lastDeparture)
            {
                throw new ArgumentException(
                    "This EAS can not be used as connection filter, the requesting algorithm needs connections after my scantime ");
            }

            if (_s.Count == 1)
            {
                throw new ArgumentException("This algorithm hasn't run yet");
            }
        }

        public bool CanBeTaken(IConnection c)
        {
            var depStation = c.ArrivalStop;
            // _s describes the latest journey we can possible take to arrive at our destination at the right time
            if (!_s.ContainsKey(depStation))
            {
                return false;
            }
            // We should arrive before the last possible moment we can still get a journey out
            return c.ArrivalTime <= _s[depStation].Time;
        }

        private Journey<T>
            GetJourneyFrom((uint, uint) location)
        {
            return _s.ContainsKey(location) ? _s[location] : Journey<T>.NegativeInfiniteJourney;
        }
    }
}