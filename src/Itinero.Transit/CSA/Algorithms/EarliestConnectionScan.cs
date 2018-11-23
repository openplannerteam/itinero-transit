using System;
using System.Collections.Generic;

namespace Itinero.Transit
{
    /// <summary>
    /// Calculates the fastest journey from A to B starting at a given time; using CSA (forward A*).
    /// It will download only the linked connections it needs.
    /// It does _not_ use footpath interlinks (yet)
    /// </summary>
    public class EarliestConnectionScan<T>
        where T : IJourneyStats<T>
    {
        private readonly List<Uri> _userTargetLocation;

        private readonly IConnectionsProvider _connectionsProvider;
        private readonly Profile<T> _profile;
        private readonly DateTime? _failMoment;

        /// <summary>
        /// This dictionary keeps, for each stop, the journey that arrives as early as possible
        /// </summary>
        private readonly Dictionary<string, Journey<T>> _s = new Dictionary<string, Journey<T>>();

        /// <summary>
        /// Keeps track of where we are on each trip, thus if we wouldn't leave a bus once we're on it
        /// </summary>
        private readonly Dictionary<string, Journey<T>> _trips = new Dictionary<string, Journey<T>>();

        public EarliestConnectionScan(Uri userDepartureLocation, Uri userTargetLocation,
            DateTime departureTime, DateTime timeOut,
            Profile<T> profile) :
            this(new List<Journey<T>> {new Journey<T>(userDepartureLocation, departureTime, profile.StatsFactory)},
                new List<Uri> {userTargetLocation}, profile, timeOut)
        {
        }


        public EarliestConnectionScan(IEnumerable<Journey<T>> userDepartureLocation,
            List<Uri> userTargetLocation, Profile<T> profile, DateTime? timeOut)
        {
            foreach (var loc in userDepartureLocation)
            {
                _s.Add(loc.Connection.ArrivalLocation().ToString(), loc);
            }

            _profile = profile;
            _userTargetLocation = userTargetLocation;
            _connectionsProvider = profile.ConnectionsProvider;
            _failMoment = timeOut;
        }

        public Journey<T> CalculateJourney()
        {
            DateTime? startTime = null;

            // A few locations will already have a start location
            foreach (var k in _s.Keys)
            {
                var j = _s[k];
                var t = j.Connection.ArrivalTime();
                if (startTime == null)
                {
                    startTime = t;
                }
                else if (t < startTime)
                {
                    startTime = t;
                }
            }

            DateTime start = startTime ?? throw new ArgumentException("Can not EAS without a start journey ");

            var timeTable = _connectionsProvider.GetTimeTable(start);
            var currentBestArrival = DateTime.MaxValue;

            while (true)
            {
                timeTable = new ValidatingTimeTable(_profile, timeTable);
                foreach (var c in timeTable.Connections())
                {
                    if (_failMoment != null && c.DepartureTime() > _failMoment)
                    {
                        throw new Exception("Timeout: could not calculate a route within the given time");
                    }

                    if (c.DepartureTime() > currentBestArrival)
                    {
                        GetBestTime(out var bestTarget);
                        return GetJourneyTo(bestTarget);
                    }

                    IntegrateConnection(c);
                }

                currentBestArrival = GetBestTime(out _);


                timeTable = _connectionsProvider.GetTimeTable(timeTable.NextTable());
            }
        }

        private DateTime GetBestTime(out Uri bestTarget)
        {
            var currentBestArrival = DateTime.MaxValue;
            bestTarget = null;
            foreach (var targetLoc in _userTargetLocation)
            {
                var arrival = GetJourneyTo(targetLoc).Connection.ArrivalTime();

                if (arrival < currentBestArrival)
                {
                    currentBestArrival = arrival;
                    bestTarget = targetLoc;
                }
            }

            return currentBestArrival;
        }


        /// <summary>
        /// Handle a single connection, update the stop positions with new times if possible
        /// </summary>
        /// <param name="c"></param>
        private void IntegrateConnection(IConnection c)
        {
            // The connection describes a random connection somewhere
            // Lets check if we can take it

            var journeyTillDeparture = GetJourneyTo(c.DepartureLocation());
            var journeyTillArrival = GetJourneyTo(c.ArrivalLocation());

            if (journeyTillDeparture
                .Equals(Journey<T>.InfiniteJourney))
            {
                // The stop where this connection starts, is not yet reachable
                // Abort
                return;
            }


            if (c.DepartureTime() < journeyTillDeparture
                    .Connection.ArrivalTime())
            {
                // This connection has already left before we can make it to the stop
                return;
            }


            // When transferring
            var t1 = Journey<T>.InfiniteJourney;

            // When resting in a trip
            var t2 = Journey<T>.InfiniteJourney;

            // When walking
            // Not enabled for EAS
            //  var t3 = Journey<T>.InfiniteJourney;

            var trip = c.Trip()?.ToString();
            if (trip != null)
            {
                if (_trips.ContainsKey(trip))
                {
                    // We could be on this trip already, lets extend the journey
                    t2 = _trips[trip] = new Journey<T>(_trips[trip], c);
                }
                else
                {
                    // We now for sure know that we can board this connection, and thus this trip
                    // This is the first encounter of it.
                    // The departure station should be stable in time, so we can take that journey and board
                    // We have to take transfertime into account though
                    var transfer =
                        _profile.CalculateInterConnection(journeyTillDeparture
                            .Connection, c);
                    if (transfer != null)
                    {
                        // We have boarded this trip!
                        t2 = _trips[trip]
                            = new Journey<T>(new Journey<T>(journeyTillDeparture
                                , transfer), c);
                    }
                }
            }

            if (!(c is IContinuousConnection)
                && journeyTillDeparture
                    .GetLastTripId() != null
                && !Equals(journeyTillDeparture
                    .Connection.Trip(), c.Trip()))
            {
                // We have to transfer vehicles
                var transfer =
                    _profile.CalculateInterConnection(journeyTillDeparture
                        .Connection, c);
                if (transfer != null)
                {
                    // Enough time to transfer
                    // It is an option

                    t1 = new Journey<T>(
                        new Journey<T>(
                            journeyTillDeparture,
                            transfer),
                        c);
                }
            }
            else
            {
                // This connection was a walk or something similar
                // Or we didn't have to transfer
                // We chain the current connection after it
                t1 = new Journey<T>(journeyTillDeparture
                    , c);
            }


            // Jej! We can take the train! 
            // Lets see if we can make an improvement in regards to the previous solution
            _s[c.ArrivalLocation().ToString()] = SelectLowest(journeyTillArrival, t1, t2);


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

        private Journey<T> SelectLowest(params Journey<T>[] journeys)
        {
            var earliest = journeys[0];
            var earliestTime = earliest.Connection.ArrivalTime();
            foreach (var journey in journeys)
            {
                if (journey.Connection.ArrivalTime() < earliestTime)
                {
                    earliest = journey;
                    earliestTime = journey.Connection.ArrivalTime();
                }
            }

            return earliest;
        }

        private Journey<T> GetJourneyTo(Uri stop)
        {
            return
                _s.ContainsKey(stop.ToString())
                    ? _s[stop.ToString()]
                    : Journey<T>.InfiniteJourney;
        }
    }
}