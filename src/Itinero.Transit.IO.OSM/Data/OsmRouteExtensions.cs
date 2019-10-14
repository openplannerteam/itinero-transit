using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Logging;
using Itinero.Transit.Utils;
using Attribute = Itinero.Transit.Data.Attributes.Attribute;

namespace Itinero.Transit.IO.OSM.Data
{
    /// <summary>
    /// Adds a OsmRoute to a transitDb
    /// </summary>
    internal static class OsmRouteExtensions
    {
        internal static void UseOsmRoute(this TransitDb tdb, OsmRoute route, DateTime start, DateTime end)
        {
            tdb.GetWriter().UseOsmRoute(tdb.DatabaseId, route, start, end);
        }

        internal static void UseOsmRoute(this TransitDbWriter wr, uint dbId, OsmRoute route, DateTime start,
            DateTime end)
        {
            Log.Information($"Adding route {route.Id} to the transitdb in frame {start} --> {end}. " +
                            $"The route {(route.RoundTrip ? "loops" : "does not loop")}, has {route.StopPositions.Count} stops, " +
                            $"is based in timezone {route.GetTimeZone()} ");
            if (route.StopPositions.Count <= 1)
            {
                throw new ArgumentException("No or only one stop positions in OSM route");
            }

            var stopIds = new List<StopId>();

            foreach (var (id, lon, lat, tags) in route.StopPositions)
            {
                var attr = new List<Attribute>();

                foreach (var tag in tags)
                {
                    attr.Add(new Attribute(tag.Key, tag.Value));
                }

                stopIds.Add(wr.AddOrUpdateStop(id, lon, lat, attr));
            }

            var allRuns = new List<(uint, Connection)>();

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


                    var tripIndex = wr.AddOrUpdateTrip(tripGlobalId, new[]
                    {
                        new Attribute("route", "http://openstreetmap.org/relation/" + route.Id),
                        new Attribute("headsign", route.Name ?? "")
                    });
                    allRuns.AddRange(
                        CreateRun(route, dbId, tripIndex, index, stopIds, currentStart));

                    index = (index + 1) % modulo;
                    currentStart += route.Interval;
                }
            }


            foreach (var (vehicle, connection) in allRuns.OrderBy(c => c.Item2.DepartureTime))
            {
                connection.GlobalId = $"https://openstreetmap.org/relation/{route.Id}/vehicle/{vehicle}/" +
                                      $"{(connection.DepartureTime - connection.DepartureDelay).FromUnixTime():s}";

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
        private static LinkedList<(uint, Connection)> CreateRun(this OsmRoute route,
            uint dbId,
            TripId tripId, uint vehicleId,
            List<StopId> locations,
            DateTime startMoment)
        {
            if (startMoment.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("startMoment: DateTimes should be UTC");
            }

            var conns = new LinkedList<(uint, Connection)>();

            var travelTime = (ushort) (route.Duration.TotalSeconds / route.StopPositions.Count);

            for (var i = 0; i < locations.Count - 1; i++)
            {
                // There are (n-1) connections between n stops; hence the 'count - 1' in the loop above
                // This implies that, if the route does do a loop (`roundtrip=yes`) the first and last element in the osmRoute should be the same

                var l0 = locations[i];
                (double lon, double lat) l0Coor = (route.StopPositions[i].lon, route.StopPositions[i].lat);
                var l0Id = Uri.EscapeDataString(route.StopPositions[i].Item1);
                var l1 = locations[i + 1];
                (double lon, double lat) l1Coor = (route.StopPositions[i + 1].lon, route.StopPositions[i + 1].lat);
                var l1Id = Uri.EscapeDataString(route.StopPositions[i + 1].Item1);

                var depTime = startMoment.AddSeconds(travelTime * i);

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


                var con = new Connection(
                    new ConnectionId(dbId, 0),
                    $"https://www.openstreetmap.org/directions?engine=fossgis_osrm_car&route={l0Coor.lat}%2C{l0Coor.lon}%3B{l1Coor.lat}%2C{l1Coor.lon}" +
                    $"&from={l0Id}&to={l1Id}",
                    l0, l1, depTime.ToUnixTime(), travelTime, 0, 0, mode, tripId);
                conns.AddLast((vehicleId, con));
            }

            return conns;
        }
    }
}