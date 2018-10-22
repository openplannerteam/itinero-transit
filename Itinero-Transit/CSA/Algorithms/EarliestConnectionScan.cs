using System;
using System.Collections.Generic;
using Itinero_Transit.CSA.ConnectionProviders;
using Serilog;

namespace Itinero_Transit.CSA
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
        private readonly DateTime? _failMoment;

        /// <summary>
        /// This dictionary keeps, for each stop, the journey that arrives as early as possible
        /// </summary>
        private readonly Dictionary<string, Journey<T>> _s = new Dictionary<string, Journey<T>>();

        public EarliestConnectionScan(Uri userDepartureLocation, DateTime departureTime,
            Uri userTargetLocation,
            T statsFactory, IConnectionsProvider connectionsProvider, DateTime? timeOut = null) :
            this(new List<Journey<T>> {new Journey<T>(userDepartureLocation, departureTime, statsFactory)},
                new List<Uri> {userTargetLocation}, connectionsProvider, timeOut)
        {
        }


        public EarliestConnectionScan(List<Journey<T>> userDepartureLocation,
            List<Uri> userTargetLocation, IConnectionsProvider connectionsProvider, DateTime? timeOut)
        {
            foreach (var loc in userDepartureLocation)
            {
                _s.Add(loc.Connection.ArrivalLocation().ToString(), loc);
            }

            _userTargetLocation = userTargetLocation;
            _connectionsProvider = connectionsProvider;
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
                var arrival = GetJourneyTo(targetLoc).Time;

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

            var journeyTillStop = GetJourneyTo(c.DepartureLocation());
            if (journeyTillStop.Equals(Journey<T>.InfiniteJourney))
            {
                // The stop where this connection starts, is not yet reachable
                // Abort
                return;
            }


            if (c.DepartureTime() <= journeyTillStop.Time)
            {
                // This connection has already left before we can make it to the stop
                return;
            }

            if (c.ArrivalTime() > GetJourneyTo(c.ArrivalLocation()).Time)
            {
                // We will arrive later to the target stop
                // It is no use to take the connection
                return;
            }

            // Jej! We can take the train! It gets us to some stop faster then previously known
            _s[c.ArrivalLocation().ToString()] = new Journey<T>(journeyTillStop, c.ArrivalTime(), c);
        }

        private Journey<T> GetJourneyTo(Uri stop)
        {
            return _s.GetValueOrDefault(stop.ToString(), Journey<T>.InfiniteJourney);
        }
    }
}