using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Processor
{
    public static class CopyDatabase
    {
        /// <summary>
        /// Copies a transitDB. Allows filtering of all aspects.
        /// Note that the predicats only indicate which values are initially copied.
        /// If a subsequent data element needs the given trip id, it will be copied as well.
        /// If a filtering predicate is not given, it is filtered by default
        /// </summary>
        public static TransitDb Copy(this TransitDb old,
            bool allowEmpty = false,
            Predicate<Stop> keepStop = null,
            Func<Stop, Stop> modifyStop = null,
            Predicate<Trip> keepTrip = null,
            Func<Trip, Trip> modifyTrip = null,
            Predicate<(Dictionary<string, StopId> stopIdMapping, Dictionary<StopId, string> reverseStopIdMapping, Dictionary<string, TripId> tripIdMapping, Dictionary<TripId, string> reverseTripIdMapping, Connection c)> keepConnection = null,
            Func<Connection, Connection> modifyConnection = null
        )
        {
            var newDb = new TransitDb(old.DatabaseId);
            var wr = newDb.GetWriter();

            if (modifyStop == null)
            {
                modifyStop = stop => stop;
            }

            if (modifyConnection == null)
            {
                modifyConnection = c => c;
            }

            if (modifyTrip == null)
            {
                modifyTrip = t => t;
            }

            var stopIdMapping = new Dictionary<string, StopId>();
            var tripIdMapping = new Dictionary<string, TripId>();
            var reverseStopIdMapping = new Dictionary<StopId, string>();
            var reverseTripIdMapping = new Dictionary<TripId, string>();

            foreach (var stop in old.Latest.StopsDb)
            {
                if (keepStop != null && !keepStop(stop))
                {
                    continue;
                }

                var id = wr.AddOrUpdateStop(modifyStop(stop));
                stopIdMapping.Add(stop.GlobalId, id);
                reverseStopIdMapping.Add(id, stop.GlobalId);
            }

            foreach (var trip in old.Latest.TripsDb)
            {
                if (keepTrip != null && !keepTrip(trip))
                {
                    continue;
                }

                var tripId = wr.AddOrUpdateTrip(trip);
                tripIdMapping.Add(trip.GlobalId, tripId);
                reverseTripIdMapping.Add(tripId, trip.GlobalId);
            }

            var copiedConnections = 0;

            foreach (var c in old.Latest.ConnectionsDb)
            {


                var depStop = old.Latest.StopsDb.Get(c.DepartureStop);
                if (!stopIdMapping.TryGetValue(depStop.GlobalId, out var depStopId))
                {
                    depStopId = wr.AddOrUpdateStop(modifyStop(depStop));
                    stopIdMapping.Add(depStop.GlobalId, depStopId);
                    reverseStopIdMapping.Add(depStopId, depStop.GlobalId);

                }

                var arrStop = old.Latest.StopsDb.Get(c.ArrivalStop);
                if (!stopIdMapping.TryGetValue(arrStop.GlobalId, out var arrStopId))
                {
                    arrStopId = wr.AddOrUpdateStop(modifyStop(arrStop));
                    stopIdMapping.Add(arrStop.GlobalId, arrStopId);
                    reverseStopIdMapping.Add(arrStopId, arrStop.GlobalId );
                }

                var trip = old.Latest.TripsDb.Get(c.TripId);
                if (!tripIdMapping.TryGetValue(trip.GlobalId, out var tripId))
                {
                    tripId = wr.AddOrUpdateTrip(modifyTrip(trip));
                    tripIdMapping.Add(trip.GlobalId, tripId);
                    reverseTripIdMapping.Add(tripId, trip.GlobalId);

                }


                var newConnection = new Connection(
                    c.GlobalId,
                    depStopId,
                    arrStopId,
                    c.DepartureTime,
                    c.TravelTime,
                    c.Mode, tripId);
                
                if (keepConnection != null && !keepConnection(
                        (stopIdMapping, reverseStopIdMapping, tripIdMapping,reverseTripIdMapping, newConnection)))
                {
                    continue;
                }

                wr.AddOrUpdateConnection(modifyConnection(newConnection));
                copiedConnections++;
            }

            wr.Close();

            if (!allowEmpty && copiedConnections == 0)
            {
                throw new ArgumentException("No connections copied");
            }

            Console.WriteLine($"Copied {copiedConnections} connections");
            return newDb;
        }
    }
}