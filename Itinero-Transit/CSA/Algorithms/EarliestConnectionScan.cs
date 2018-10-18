﻿using System;
using System.Collections.Generic;
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
        private readonly Uri _userDepartureLocation;
        private readonly Uri _userTargetLocation;

        private readonly IConnectionsProvider _connectionsProvider;

        /// <summary>
        /// This dictionary keeps, for each stop, the journey that arrives as early as possible
        /// </summary>
        private readonly Dictionary<Uri, Journey<T>> _s = new Dictionary<Uri, Journey<T>>();

        private readonly IJourneyStats<T> _statsFactory;

        public EarliestConnectionScan(Uri userDepartureLocation, Uri userTargetLocation,
            IJourneyStats<T> statsFactory, IConnectionsProvider connectionsProvider)
        {
            _userDepartureLocation = userDepartureLocation;
            _userTargetLocation = userTargetLocation;
            _statsFactory = statsFactory;
            _connectionsProvider = connectionsProvider;
        }

        public Journey<T> CalculateJourney(DateTime departureTime)
        {
            var timeTable = _connectionsProvider.GetTimeTable(_connectionsProvider.TimeTableIdFor(departureTime));
            var currentBestArrival = DateTime.MaxValue;

            while (true)
            {
                foreach (var c in timeTable.Connections())
                {
                    if (c.DepartureTime() > currentBestArrival)
                    {
                        return GetJourneyTo(_userTargetLocation);
                    }


                    IntegrateConnection(c);
                    currentBestArrival = GetJourneyTo(_userTargetLocation).Time;
                }

                timeTable = _connectionsProvider.GetTimeTable(timeTable.NextTable());
            }
        }


        /// <summary>
        /// Handle a single connection, update the stop positions with new times if possible
        /// </summary>
        /// <param name="c"></param>
        private void IntegrateConnection(IConnection c)
        {
            if (c.DepartureLocation().Equals(_userDepartureLocation))
            {
                Log.Information("Found a connection away!");
                // Special case: we can always take this connection as we start here
                // If the arrival stop can be reached faster then previously known, we take the trip
                var actualArr = c.ArrivalTime();
                if (actualArr >= GetJourneyTo(c.ArrivalLocation()).Time) return;

                // Yey! We arrive earlier then previously known
                _s[c.ArrivalLocation()] = new Journey<T>(_statsFactory.InitialStats(c), actualArr, c);

                // All done with this connection
                return;
            }


            // The connection describes a random connection somewhere
            // Lets check if we can take it

            var journeyTillStop = GetJourneyTo(c.DepartureLocation());
            if (journeyTillStop.Equals(Journey<T>.InfiniteJourney))
            {
                //    Log.Information("Stop not yet reachable");
                // The stop where connection starts, is not yet reachable
                // Abort
                return;
            }


            if (c.DepartureTime() < journeyTillStop.Time)
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
            _s[c.ArrivalLocation()] = new Journey<T>(journeyTillStop, c.ArrivalTime(), c);
        }

        private Journey<T> GetJourneyTo(Uri stop)
        {
            return _s.GetValueOrDefault(stop, Journey<T>.InfiniteJourney);
        }
    }
}