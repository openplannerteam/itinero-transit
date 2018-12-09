using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Tiles;

namespace Itinero.Transit.Algorithms.Search
{
    public static class StopSearch
    {
        /// <summary>
        /// Enumerates all stops in the given bounding box.
        /// </summary>
        /// <param name="stopsDb">The stops db.</param>
        /// <param name="box">The box to enumerate in.</param>
        /// <returns>An enumerator with all the stops.</returns>
        public static IEnumerable<IStop> SearchStopsInBox(this StopsDb stopsDb,
            (double minLon, double minLat, double maxLon, double maxLat) box)
        {
            var range = new TileRange(box, stopsDb.StopLocations.Zoom);
            var tileRangeLocationEnumerator = stopsDb.StopLocations.GetTileRangeEnumerator(range);
            var rangeStops = new TileRangeStopEnumerable(stopsDb, tileRangeLocationEnumerator);
            using (var rangeStopsEnumerator = rangeStops.GetEnumerator())
            {
                while (rangeStopsEnumerator.MoveNext())
                {
                    var location = rangeStopsEnumerator.TileRangeLocationEnumerator;
                    if (box.minLat > location.Latitude ||
                        box.minLon > location.Longitude ||
                        box.maxLat < location.Latitude ||
                        box.maxLon < location.Longitude)
                    {
                        continue;
                    }
                    yield return rangeStopsEnumerator.Current;
                }
            }
        }
    }
}