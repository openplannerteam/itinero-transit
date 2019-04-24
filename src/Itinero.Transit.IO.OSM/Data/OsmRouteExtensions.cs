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
    public static class OsmRouteExtensions
    {
        public static void UseOsmRoute(this TransitDb tdb, Uri url, DateTime start, DateTime end)
        {
            var r = OsmRoute.LoadFromUrl(url);
            foreach (var route in r)
            {
                tdb.UseOsmRoute(route, start, end);
            }
        }

        public static void UseOsmRoute(this TransitDb tdb, string filePath, DateTime start, DateTime end)
        {
            var r = OsmRoute.LoadFromFile(filePath);
            foreach (var route in r)
            {
                tdb.UseOsmRoute(route, start, end);
            }
        }

        public static void UseOsmRoute(this TransitDb tdb, long id, DateTime start, DateTime end)
        {
            var r = OsmRoute.LoadFromOsm(id);
            foreach (var route in r)
            {
                tdb.UseOsmRoute(route, start, end);
            }
        }


        private static void UseOsmRoute(this TransitDb tdb, OsmRoute route, DateTime start, DateTime end)
        {
            Log.Information($"Adding route {route.Id} to the transitdb in frame {start} --> {end}");

            if (route.StopPositions.Count <= 1)
            {
                throw new ArgumentException("No or only one stop positions in OSM route");
            }

            var wr = tdb.GetWriter();
            var stopIds = new List<LocationId>();

            foreach (var (id, coordinate, tags) in route.StopPositions)
            {
                var attr = new List<Attribute>();

                foreach (var tag in tags)
                {
                    attr.Add(new Attribute(tag.Key, tag.Value));
                }

                stopIds.Add(wr.AddOrUpdateStop(id, coordinate.X, coordinate.Y, attr));
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
                    Log.Verbose($"There will be {modulo} vehicles");
                }


                while (currentStart <= end)
                {
                    if (!route.OpeningTimes.StateAt(currentStart, "closed").Equals("open"))
                    {
                        currentStart = route.OpeningTimes.NextChange(currentStart);
                        continue;
                    }

                    var tripGlobalId = $"https://openstreetmap.org/relation/{route.Id}/vehicle/{index}";
                    var tripIndex = wr.AddOrUpdateTrip(tripGlobalId);
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
        private static LinkedList<(uint, IConnection)> CreateRun(this OsmRoute route, uint internalTripId, uint vehicleId, List<LocationId> locations,
            DateTime startMoment)
        {
            var conns = new LinkedList<(uint, IConnection)>();

            var travelTime = (ushort) (route.Duration.TotalSeconds / route.StopPositions.Count);

            for (var i = 0; i < locations.Count - 1; i++)
            {
                var l0 = locations[i];
                var l1 = locations[i + 1];

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


                var con = new SimpleConnection(0, l0, l1, depTime.ToUnixTime(), travelTime, 0, 0, mode, internalTripId);
                conns.AddLast((vehicleId, con));
            }

            return conns;
        }
    }
}