using System;
using System.Runtime.CompilerServices;
using Itinero.Transit.Data;
using Itinero.Transit.Utils;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]

namespace Itinero.Transit.Algorithms.Search
{
    internal static class StopSearch
    {

        private const double _radiusOfEarth = 6371000;

        /// <summary>
        /// Returns an estimate of the distance between the two given coordinates.
        /// </summary>
        /// <remarks>Accuracy decreases with distance.</remarks>
        internal static double DistanceEstimateInMeter(double longitude1, double latitude1, double longitude2,
            double latitude2)
        {
            var lat1Rad = (latitude1 / 180d) * Math.PI;
            var lon1Rad = (longitude1 / 180d) * Math.PI;
            var lat2Rad = (latitude2 / 180d) * Math.PI;
            var lon2Rad = (longitude2 / 180d) * Math.PI;

            var x = (lon2Rad - lon1Rad) * Math.Cos((lat1Rad + lat2Rad) / 2.0);
            var y = lat2Rad - lat1Rad;

            var m = Math.Sqrt(x * x + y * y) * _radiusOfEarth;

            return m;
        }



        public static float CalculateDistanceBetween
            (IStopsReader reader, StopId departureLocation, StopId targetLocation)
        {
            if (!reader.Id.Equals(targetLocation))
            {
                reader.MoveTo(targetLocation);
            }
            var lat1 = reader.Latitude;
            var lon1 = reader.Longitude;


            reader.MoveTo(departureLocation);
            var lat0 = reader.Latitude;
            var lon0 = reader.Longitude;


            var distance = DistanceEstimate.DistanceEstimateInMeter(
                lat0, lon0, lat1, lon1);
            return distance;
        }
    }
}