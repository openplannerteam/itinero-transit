using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Transit.Algorithms.Search;
using Itinero.Transit.Data.Walks;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]
namespace Itinero.Transit.Data
{
    /// <summary>
    /// Contains extension methods for the stops db.
    /// </summary>
    internal static class StopsDbExtensions
    {
        public static IEnumerable<IStop> LocationsInRange(
            this StopsDb stopsDb, IStop stop, float maxDistance)
        {
            var lat = (float) stop.Latitude;
            var lon = (float) stop.Longitude;
            return stopsDb.LocationsInRange(lat, lon, maxDistance);
        }

        public static IEnumerable<IStop> LocationsInRange(
            this StopsDb stopsDb, float lat, float lon, float maxDistance)
        {
            var box = (
                DistanceEstimate.MoveEast(lat, lon, -maxDistance), // minLon
                DistanceEstimate.MoveNorth(lat, lon, +maxDistance), // MinLat
                DistanceEstimate.MoveEast(lat, lon, +maxDistance), // MaxLon
                DistanceEstimate.MoveNorth(lat, lon, -maxDistance) //maxLat
            );
            return stopsDb.SearchInBox(box);
        }


        public static float CalculateDistanceBetween
            (this StopsDb.StopsDbReader reader, (uint, uint) departureLocation, (uint, uint) targetLocation)
        {
            reader.MoveTo(departureLocation);
            var lat0 = (float) reader.Latitude;
            var lon0 = (float) reader.Longitude;

            reader.MoveTo(targetLocation);
            var lat1 = (float) reader.Latitude;
            var lon1 = (float) reader.Longitude;

            var distance = DistanceEstimate.DistanceEstimateInMeter(
                lat0, lon0, lat1, lon1);
            return distance;
        }
    }
}