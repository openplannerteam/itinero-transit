using System;
using System.Collections.Generic;
using Itinero.Transit.Logging;
using Attribute = Itinero.Transit.Data.Attributes.Attribute;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// Adds a OsmRoute to a transitDb
    /// </summary>
    public static class OsmRouteExtensions
    {
        public static void AddOsmRoute(this TransitDb tdb, OsmRoute route, DateTime start, DateTime end)
        {
            if (route.StopPositions.Count == 0)
            {
                throw new ArgumentException("No stop positions in OSM route");
            }

            if (route.StopPositions.Count == 1)
            {
                throw new ArgumentException("Only one stop positions in OSM route");
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

            /*
             * The idea is: we simulate the buses driving over this route
             * We start simulating during 'opening-hours' and run the shuttles as needed.
             * We create shuttles as needed (every 'interval' length).
             * If round trip is specified, we reuse the shuttle indexes
             */

            var currentStart = start;
            var index = 0;
            var modulo = int.MaxValue;

            if (route.RoundTrip)
            {
                modulo = (int) Math.Ceiling(route.Duration.TotalSeconds / route.Interval.TotalSeconds);
                Log.Verbose($"There will be {modulo} vehicles");
            }


            while (currentStart <= end)
            {
                if (!route.OpeningTimes.StateAt(currentStart, "closed").Equals("open"))
                {
                    currentStart = route.OpeningTimes.NextChange(currentStart);
                    continue;
                }


                AddRun(wr, stopIds, route, index.ToString(), currentStart);

                index = (index + 1) % modulo;
                currentStart += route.Interval;
            }
        }


        /// <summary>
        /// Adds a single run, from the first stop in the relation till the last.
        ///
        /// For now, the vehicle is assumed to take the same amount of time between each stop
        /// 
        /// </summary>
        /// <param name="writer">Add the run to this transitdb</param>
        /// <param name="route">The route with all the stops</param>
        /// <param name="tripIndex">The index of the trip. If the route does do roundtrips, this tripIndex might be reused</param>
        /// <param name="stopDurationSeconds">How long the vehicle waits at a stop</param>
        private static void AddRun(this TransitDb.TransitDbWriter writer, List<LocationId> locations, OsmRoute route,
            string tripIndex,
            DateTime startMoment,
            int stopDurationSeconds = 60)
        {
            var tripGlobalId = $"https://openstreetmap.org/relation/{route.Id}/vehicle/{tripIndex}";
            Log.Verbose($"Creating vehicle run {tripGlobalId} starting at {startMoment}");
            var tripId = writer.AddOrUpdateTrip(tripGlobalId);

            var travelTime = (ushort) (route.Duration.TotalSeconds / route.StopPositions.Count);

            for (var i = 0; i < locations.Count - 1; i++)
            {
                var l0 = locations[i];
                var l1 = locations[i + 1];

                var depTime = startMoment.AddSeconds(travelTime * i);
                var connectionId = $"{tripGlobalId}/{depTime:s}";

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


                writer.AddOrUpdateConnection(l0, l1, connectionId, depTime, travelTime, 0, 0, tripId, mode);
            }
        }
    }
}