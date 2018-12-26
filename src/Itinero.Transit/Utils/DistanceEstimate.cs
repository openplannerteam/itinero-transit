using System;

namespace Itinero.Transit.Data.Walks
{
    public static class DistanceEstimate
    {
        private const double RadiusOfEarth = 6371000;

        /// <summary>
        /// Returns an estimate of the distance between the two given coordinates.
        /// Stolen from https://github.com/itinero/routing/blob/1764afc75db43a1459789592de175283f642123f/src/Itinero/LocalGeo/Coordinate.cs
        /// </summary>
        /// <remarks>Accuraccy decreases with distance.</remarks>
        public static float DistanceEstimateInMeter(float latitude1, float longitude1, float latitude2,
            float longitude2)
        {
            var lat1Rad = (latitude1 / 180d) * System.Math.PI;
            var lon1Rad = (longitude1 / 180d) * System.Math.PI;
            var lat2Rad = (latitude2 / 180d) * System.Math.PI;
            var lon2Rad = (longitude2 / 180d) * System.Math.PI;

            var x = (lon2Rad - lon1Rad) * System.Math.Cos((lat1Rad + lat2Rad) / 2.0);
            var y = lat2Rad - lat1Rad;

            var m = System.Math.Sqrt(x * x + y * y) * RadiusOfEarth;

            return (float) m;
        }

        /// <summary>
        /// Gives the latitude when moving north for 'meters'
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="meters"></param>
        /// <returns></returns>
        public static float MoveNorth(float lat, float lon, float meters)
        {
            var dLat = -meters / RadiusOfEarth;
            return (float) (lat + dLat * 180 / Math.PI);
        }
        
        /// <summary>
        /// Gives the longitude when moving east
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="meters"></param>
        /// <returns></returns>
        public static float MoveEast(float lat, float lon, float meters)
        {
            var dLon = meters/(RadiusOfEarth*Math.Cos(Math.PI*lat/180));
            return (float) (lon + dLon * 180 / Math.PI);
        }
    }
}