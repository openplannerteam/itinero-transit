using System;
using System.Collections.Generic;
using Serilog;

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
            
            // Seen connections
            // TODO Remove when upstream bug is fixed
            var seen = new Dictionary<string, ITimeTable>();
            
            while (true)
            {
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


                    if (seen.ContainsKey(c.ToString()))
                    {
                        Log.Warning($"Skipping already seen connection {c.Id()}");
                        continue;
                    }

                    seen.Add(c.ToString(), timeTable);

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

            var trip = c.Trip()?.ToString();
            if (trip != null)
            {
                if (_trips.ContainsKey(trip))
                {// We can be on this trip, lets extend the journey
                    
                    
                    _trips[trip] = new Journey<T>(_trips[trip], c);

                    var arrDest = c.ArrivalLocation().ToString();
                    if (!_s.ContainsKey(arrDest)
                        || _s[arrDest].Connection.ArrivalTime() > c.ArrivalTime())
                    {
                        // We can reach this destination by using this trip quicker then previously known
                        _s[arrDest] = _trips[trip];
                    }
                }
            }

            var journeyTillStop = GetJourneyTo(c.DepartureLocation());
            if (journeyTillStop.Equals(Journey<T>.InfiniteJourney))
            {
                // The stop where this connection starts, is not yet reachable
                // Abort
                return;
            }


            if (c.DepartureTime() < journeyTillStop.Connection.ArrivalTime())
            {
                // This connection has already left before we can make it to the stop
                return;
            }

            if (!(c is IContinuousConnection)
                && journeyTillStop.GetLastTripId() != null &&
                !Equals(journeyTillStop.Connection.Trip(), c.Trip()))
            {
                // We have to transfer vehicles
                var transfer =
                    _profile.CalculateInterConnection(journeyTillStop.Connection, c);
                if (transfer == null)
                {
                    // Not enough time to transfer
                    return;
                }
            }

            if (c.ArrivalTime() > GetJourneyTo(c.ArrivalLocation()).Connection.DepartureTime())
            {
                // We will arrive later to the target stop
                // It is no use to take the connection
                return;
            }

            // Jej! We can take the train! It gets us to some stop faster then previously known
            _s[c.ArrivalLocation().ToString()] = new Journey<T>(journeyTillStop, c);

            if (trip != null && !_trips.ContainsKey(trip))
            {
                _trips[trip] =  _s[c.ArrivalLocation().ToString()];
            }

            // Alright, we are arriving at a new location. This means that we can walk from this location onto new stops

            if (c is IContinuousConnection)
            {
                return;
            }

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

        private Journey<T> GetJourneyTo(Uri stop)
        {
            return _s.GetValueOrDefault(stop.ToString(), Journey<T>.InfiniteJourney);
        }
    }
}