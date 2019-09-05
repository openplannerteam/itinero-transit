using System;

namespace Itinero.Transit.Utils
{
    public static class DistanceEstimate
    {
        // TODO: make this either internal or use lon/lat.
        
        private const double _radiusOfEarth = 6371000;

        /// <summary>
        /// Returns an estimate of the distance between the two given coordinates.
        /// Stolen from https://github.com/itinero/routing/blob/1764afc75db43a1459789592de175283f642123f/src/Itinero/LocalGeo/Coordinate.cs
        /// </summary>
        /// <remarks>Accuracy decreases with distance.</remarks>
        public static float DistanceEstimateInMeter(double latitude1, double longitude1, double latitude2,
            double longitude2)
        {
            var lat1Rad = (latitude1 / 180d) * Math.PI;
            var lon1Rad = (longitude1 / 180d) * Math.PI;
            var lat2Rad = (latitude2 / 180d) * Math.PI;
            var lon2Rad = (longitude2 / 180d) * Math.PI;

            var x = (lon2Rad - lon1Rad) * Math.Cos((lat1Rad + lat2Rad) / 2.0);
            var y = lat2Rad - lat1Rad;

            var m = Math.Sqrt(x * x + y * y) * _radiusOfEarth;

            return (float) m;
        }

        /// <summary>
        /// Gives the latitude when moving north for 'meters'
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="meters"></param>
        /// <returns></returns>
        public static double MoveNorth(double lat, double lon, double meters)
        {
            var dLat = -meters / _radiusOfEarth;
            return lat + dLat * 180 / Math.PI;
        }
        
        /// <summary>
        /// Gives the longitude when moving east
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="meters"></param>
        /// <returns></returns>
        public static double MoveEast(double lat, double lon, double meters)
        {
            var dLon = meters/(_radiusOfEarth*Math.Cos(Math.PI*lat/180));
            return lon + dLon * 180 / Math.PI;
        }
    }
}