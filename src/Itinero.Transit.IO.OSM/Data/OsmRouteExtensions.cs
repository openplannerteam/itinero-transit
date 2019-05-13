using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Logging;
using Attribute = Itinero.Transit.Data.Attributes.Attribute;

namespace Itinero.Transit.Data
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

        internal static void UseOsmRoute(this TransitDb.TransitDbWriter wr, OsmRoute route, DateTime start,
            DateTime end)
        {
            Log.Information($"Adding route {route.Id} to the transitdb in frame {start} --> {end}. " +
                            $"The route {(route.RoundTrip ? "loops" : "does not loop")}, has {route.StopPositions.Count} stops, " +
                            $"is based in timezone {route.GetTimeZone()} ");
            if (route.StopPositions.Count <= 1)
            {
                throw new ArgumentException("No or only one stop positions in OSM route");
            }

            var stopIds = new List<LocationId>();

            foreach (var (id, coordinate, tags) in route.StopPositions)
            {
                var attr = new List<Attribute>();

                foreach (var tag in tags)
                {
                    attr.Add(new Attribute(tag.Key, tag.Value));
                }

                stopIds.Add(wr.AddOrUpdateStop(id, coordinate.Y, coordinate.X, attr));
            }


            // TODO this might not scale
            var allRuns = new List<(uint, IConnection)>();

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
                    if (!route.OpeningTimes.StateAt(currentStart, "closed").Equals("open"))
                    {
                        currentStart = route.OpeningTimes.NextChange(currentStart);
                        continue;
                    }

                    var tripGlobalId = $"https://openstreetmap.org/relation/{route.Id}/vehicle/{index}";


                    var tripIndex = wr.AddOrUpdateTrip(tripGlobalId, new[]
                    {
                        new Attribute("route", "http://openstreetmap.org/relation/" + route.Id),
                        new Attribute("headsign", route.Name ?? "")
                    });    
                    allRuns.AddRange(
                        CreateRun(route, tripIndex, index, stopIds, currentStart));

                    index = (index + 1) % modulo;
                    currentStart += route.Interval;
                }
            }


            foreach (var (vehicle, connection) in allRuns.OrderBy(c => c.Item2.DepartureTime))
            {
                var connGlobalId = $"https://openstreetmap.org/relation/{route.Id}/vehicle/{vehicle}/" +
                                   $"{(connection.DepartureTime - connection.DepartureDelay).FromUnixTime():s}";

                wr.AddOrUpdateConnection(connGlobalId, connection);
            }


            wr.Close();
        }


        ///  <summary>
        ///  Creates all connections of a single run
        /// 
        ///  For now, the vehicle is assumed to take the same amount of time between each stop
        ///  
        ///  </summary>
        private static LinkedList<(uint, IConnection)> CreateRun(this OsmRoute route,
            TripId tripId, uint vehicleId,
            List<LocationId> locations,
            DateTime startMoment)
        {
            var conns = new LinkedList<(uint, IConnection)>();

            var travelTime = (ushort) (route.Duration.TotalSeconds / route.StopPositions.Count);

            for (var i = 0; i < locations.Count - 1; i++)
            {
                var l0 = locations[i];
                var l0Coor = route.StopPositions[i].Item2;
                var l0Id = Uri.EscapeDataString(route.StopPositions[i].Item1);
                var l1 = locations[i + 1];
                var l1Coor = route.StopPositions[i + 1].Item2;
                var l1Id = Uri.EscapeDataString(route.StopPositions[i + 1].Item1);

                var depTime = startMoment.AddSeconds(travelTime * i);

                ushort mode = 0;

                if (!route.RoundTrip)
                {
                    if (i == 0)
                    {
                        // Getting up only
                        mode = ConnectionExtensions.ModeGetOnOnly;
                    }

                    if (i == locations.Count - 2)
                    {
                        mode = ConnectionExtensions.ModeGetOffOnly;
                    }
                }


                var con = new SimpleConnection(0,
                    $"https://www.openstreetmap.org/directions?engine=fossgis_osrm_car&route={l0Coor.Y}%2C{l0Coor.X}%3B{l1Coor.Y}%2C{l1Coor.X}" +
                    $"&from={l0Id}&to={l1Id}",
                    l0, l1, depTime.ToUnixTime(), travelTime, 0, 0, mode, tripId);
                conns.AddLast((vehicleId, con));
            }

            return conns;
        }
    }
}