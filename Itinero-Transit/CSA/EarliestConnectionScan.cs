using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using Serilog;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// Calculates the fastest journey from A to B starting at a given time; using CSA (forward A*).
    /// It will download only the linked connections it needs.
    /// It does _not_ use footpath interlinks (yet)
    /// </summary>
    public class EarliestConnectionScan
    {
        private readonly DateTime userDepartureTime;
        private readonly Uri userDepartureStop;
        private readonly Uri userTargetStop;

        /// <summary>
        /// This dictionary keeps, for each stop, the journey that arrives as early as possible
        /// </summary>
        private Dictionary<Uri, Journey> S = new Dictionary<Uri, Journey>();

        public EarliestConnectionScan(DateTime userDepartureTime, Uri userDepartureStop, Uri userTargetStop)
        {
            this.userDepartureTime = userDepartureTime;
            this.userDepartureStop = userDepartureStop;
            this.userTargetStop = userTargetStop;
        }


        public Journey CalculateJourney(Uri startPage)
        {
            var tt = new TimeTable(startPage);
            tt.Download();

            var currentBestArrival = DateTime.MaxValue;
            
            while (true)
            {
                foreach (var c in tt.Graph)
                {
                    if (c.DepartureTime > currentBestArrival)
                    {
                        return GetJourneyTo(userTargetStop);
                    }
                    
                    
                    IntegrateConnection(c);
                    currentBestArrival = GetJourneyTo(userTargetStop).ArrivalTime;
                }

                tt = new TimeTable(tt.Next);
                tt.Download();

            }
        }


        /// <summary>
        /// Handle a single connection, update the stop positions with new times if possible
        /// </summary>
        /// <param name="c"></param>
        private void IntegrateConnection(Connection c)
        {
            if (c.DepartureStop.Equals(userDepartureStop))
            {
                Log.Information("Found a connection away!");
                // Special case: we can always take this connection as we start here
                // If the arrival stop can be reached faster then previously known, we take the trip
                var actualArr = c.ArrivalTime.AddSeconds(c.ArrivalDelay);
                if (actualArr >= GetJourneyTo(c.ArrivalStop).ArrivalTime) return;

                // Yey! We arrive earlier then previously known
                Log.Information($"We can leave to {c.ArrivalStop} where we arrive at {actualArr}");
                S[c.ArrivalStop] = new Journey(null, actualArr, c);

                // All done with this connection
                return;
            }


            // The connection describes a random connection somewhere
            // Lets check if we can take it

            var journeyTillStop = GetJourneyTo(c.DepartureStop);
            if (journeyTillStop.Equals(Journey.InfiniteJourney))
            {
                //    Log.Information("Stop not yet reachable");
                // The stop where connection starts, is not yet reachable
                // Abort
                return;
            }


            if (c.DepartureTime < journeyTillStop.ArrivalTime)
            {
                // This connection has already left before we can make it to the stop
                return;
            }

            var actualArrival = c.ArrivalTime.AddSeconds(c.ArrivalDelay);
            if (actualArrival > GetJourneyTo(c.ArrivalStop).ArrivalTime)
            {
                // We will arrive later to the target stop
                // It is no use to take the connection
                return;
            }

            Log.Information($"New stop! We can now reach {c.ArrivalStop} at a time of {actualArrival}");

            // Jej! We can take the train! It gets us to some stop faster then previously known
            S[c.ArrivalStop] = new Journey(journeyTillStop, actualArrival, c);
        }

        private Journey GetJourneyTo(Uri stop)
        {
            return S.GetValueOrDefault(stop, Journey.InfiniteJourney);
        }
    }
}