using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Logging;
using Itinero.Transit.Utils;

namespace Itinero.Transit.IO.OSM.Data
{
    /// <summary>
    /// Adds a OsmRoute to a transitDb
    /// </summary>
    internal static class OsmRouteExtensions
    {
        internal static void UseOsmRoute(this TransitDb tdb, OsmRoute route, DateTime start, DateTime end)
        {
            tdb.GetWriter().UseOsmRoute(route, start, end);
        }

        internal static void UseOsmRoute(this TransitDbWriter wr, OsmRoute route, 
            DateTime start,DateTime end)
        {

            wr.GlobalId = "https://osm.org/relation/"+route.Id;
            wr.AttributesWritable["name"] = route.Name;
            wr.AttributesWritable["duration"] = ""+route.Duration;
            wr.AttributesWritable["interval"] = ""+route.Interval;
            wr.AttributesWritable["roundtrip"] = ""+route.RoundTrip;
            wr.AttributesWritable["stops:count"] = ""+route.StopPositions.Count;
            wr.AttributesWritable["stops"] = string.Join(";", route.StopPositions.Select(stop => stop.url));

            Log.Information($"Adding route {route.Id} to the transitdb in frame {start} --> {end}. " +
                            $"The route {(route.RoundTrip ? "loops" : "does not loop")}, has {route.StopPositions.Count} stops, " +
                            $"is based in timezone {route.GetTimeZone()}. Opening hours are not taken into account in this version of Itinero-Transit");
            if (route.StopPositions.Count <= 1)
            {
                throw new ArgumentException("No or only one stop positions in OSM route");
            }

            var stopIds = new List<StopId>();

            foreach (var (id, lon, lat, tags) in route.StopPositions)
            {
                var attrs = new Dictionary<string, string>();

                foreach (var tag in tags)
                {
                    attrs[tag.Key] = tag.Value;
                }

                stopIds.Add(wr.AddOrUpdateStop(new Stop(id, (lon, lat), attrs)));
            }

            var allRuns = new List<Connection>();

            {
                // Simulate the buses running.
                // Every 'interval' time, we create a shuttle doing the entire route
                // If the route roundtrips, then index numbers (and thus tripIDs) are recycled - allowing someone to drive 'over' the first stop

                // When a single trip is simulated, all the connections are collected
                // They are sorted afterwards and added to the tdb

                var currentStart = start;
                uint index = 0;
                var modulo = uint.MaxValue;

                if (route.RoundTrip)
                {
                    modulo = (uint) Math.Ceiling(route.Duration.TotalSeconds / route.Interval.TotalSeconds);
                }


                while (currentStart <= end)
                {
                    var tripGlobalId = $"https://openstreetmap.org/relation/{route.Id}/vehicle/{index}";


                    var tripIndex = wr.AddOrUpdateTrip(new Trip(tripGlobalId, OperatorId.Invalid, new Dictionary<string, string>
                    {
                        {"route", "http://openstreetmap.org/relation/" + route.Id},
                        {"headsign", route.Name ?? ""}
                    }));
                    allRuns.AddRange(
                        CreateRun(route, tripIndex, index, stopIds, currentStart));

                    index = (index + 1) % modulo;
                    currentStart += route.Interval;
                }
            }


            foreach (var  connection in allRuns.OrderBy(c => c.DepartureTime))
            {
                wr.AddOrUpdateConnection(connection);
            }

            wr.Close();
        }


        ///  <summary>
        ///  Creates all connections of a single run
        /// 
        ///  For now, the vehicle is assumed to take the same amount of time between each stop
        ///  
        ///  </summary>
        private static IEnumerable<Connection> CreateRun(this OsmRoute route,
            TripId tripId, uint vehicleId,
            IReadOnlyList<StopId> locations,
            DateTime startMoment)
        {
            if (startMoment.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("startMoment: DateTimes should be UTC");
            }

            var conns = new LinkedList<Connection>();

            var travelTime = (ushort) (route.Duration.TotalSeconds / route.StopPositions.Count);

            for (var i = 0; i < locations.Count - 1; i++)
            {
                // There are (n-1) connections between n stops; hence the 'count - 1' in the loop above
                // This implies that, if the route does do a loop (`roundtrip=yes`) the first and last element in the osmRoute should be the same

                var l0 = locations[i];
                var l1 = locations[i + 1];

                var depTime = startMoment.AddSeconds(travelTime * i);
                var id = $"https://openstreetmap.org/relation/{route.Id}/vehicle/{vehicleId}/" +
                         $"{depTime:s}";


                ushort mode = 0;

                if (!route.RoundTrip)
                {
                    if (i == 0)
                    {
                        // Getting up only
                        mode = Connection.ModeGetOnOnly;
                    }

                    if (i == locations.Count - 2)
                    {
                        mode = Connection.ModeGetOffOnly;
                    }
                }

                var con = new Connection(id ,l0, l1, depTime.ToUnixTime(), travelTime, mode, tripId);
                conns.AddLast(con);
            }

            return conns;
        }
    }
}