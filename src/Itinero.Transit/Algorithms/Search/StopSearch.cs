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
        public static IEnumerable<IStop> SearchInBox(this IStopsReader stopsDb,
            (double minLon, double minLat, double maxLon, double maxLat) box)
        {
            var range = new TileRange(box, stopsDb.StopsDb.StopLocations.Zoom);
            var tileRangeLocationEnumerator = stopsDb.StopsDb.StopLocations.GetTileRangeEnumerator(range);
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
        
        /// <summary>
        /// Enumerates all stops in the given bounding box.
        /// </summary>
        /// <param name="stopsDb">The stops db.</param>
        /// <param name="lon">The longitude.</param>
        /// <param name="lat">The latitude.</param>
        /// <param name="maxDistanceInMeters">The maximum distance in meters.</param>
        /// <returns>An enumerator with all the stops.</returns>
        public static IStop SearchClosest(this IStopsReader stopsDb,
            double lon, double lat, double maxDistanceInMeters = 1000)
        {
            var bestDistance = maxDistanceInMeters;
            IStop bestStop = null;
            
            var box = BoxWithSize((lon, lat), bestDistance);
            var stops = stopsDb.SearchInBox(box);
            foreach (var stop in stops)
            {
                var stopDistance = DistanceEstimateInMeter(lon, lat, stop.Longitude, stop.Latitude);
                if (!(stopDistance < bestDistance)) continue;
                bestStop = stop;
                bestDistance = stopDistance;
            }

            return bestStop;
        }

        private const double _radiusOfEarth = 6371000;

        /// <summary>
        /// Returns an estimate of the distance between the two given coordinates.
        /// </summary>
        /// <remarks>Accuracy decreases with distance.</remarks>
        private static double DistanceEstimateInMeter(double longitude1, double latitude1, double longitude2, double latitude2)
        {
            var lat1Rad = (latitude1 / 180d) * System.Math.PI;
            var lon1Rad = (longitude1 / 180d) * System.Math.PI;
            var lat2Rad = (latitude2 / 180d) * System.Math.PI;
            var lon2Rad = (longitude2 / 180d) * System.Math.PI;

            var x = (lon2Rad - lon1Rad) * System.Math.Cos((lat1Rad + lat2Rad) / 2.0);
            var y = lat2Rad - lat1Rad;

            var m = System.Math.Sqrt(x * x + y * y) * _radiusOfEarth;

            return m;
        }
        
        /// <summary>
        /// Offsets this coordinate for a given distance in a given direction.
        /// </summary>
        private static (double minLon, double minLat, double maxLon, double maxLat) BoxWithSize(this (double lon, double lat) location,
            double distance)
        {
            var ratioInRadians = distance / _radiusOfEarth;

            var oldLatRadians = location.lat.ToRadians();
            var oldLonRadians = location.lon.ToRadians();
            var bearing = ((double)45).ToRadians();

            var maxLatRadians = System.Math.Asin(
                System.Math.Sin(oldLatRadians) *
                System.Math.Cos(ratioInRadians) +
                System.Math.Cos(oldLatRadians) *
                System.Math.Sin(ratioInRadians) *
                System.Math.Cos(bearing));

            var maxLonRadians = oldLonRadians + System.Math.Atan2(
                                   System.Math.Sin(bearing) *
                                   System.Math.Sin(ratioInRadians) *
                                   System.Math.Cos(oldLatRadians),
                                   System.Math.Cos(ratioInRadians) -
                                   System.Math.Sin(oldLatRadians) *
                                   System.Math.Sin(maxLatRadians));
            
            var maxLat = maxLatRadians.ToDegrees();
            if (maxLat > 180)
            {
                maxLat = maxLat - 360;
            }
            var minLat = location.lat - (maxLat - location.lat);
            
            var maxLon = maxLonRadians.ToDegrees();
            if (maxLon > 180)
            {
                maxLon = maxLon - 360;
            }
            var minLon = location.lon - (maxLon - location.lon);
            
            return (minLon, minLat, maxLon, maxLat);
        }
        
        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        private static double ToRadians(this double degrees)
        {
            return (degrees / 180d) * System.Math.PI;
        }
        
        /// <summary>
        /// Converts radians to degrees.
        /// </summary>
        private static double ToDegrees(this double radians)
        {
            return (radians / System.Math.PI) * 180d;
        }
    }
}