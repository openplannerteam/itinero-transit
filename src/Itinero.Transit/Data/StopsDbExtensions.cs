using System.Collections.Generic;
using System.Linq.Expressions;
using Itinero.Transit.Algorithms.Search;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.Data.Walks;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// Contains extension methods for the stops db.
    /// </summary>
    public static class StopsDbExtensions
    {
        /// <summary>
        /// Adds a new stop and returns it's internal id.
        /// </summary>
        /// <param name="stopsDb">The stops db.</param>
        /// <param name="globalId">The global stop id.</param>
        /// <param name="longitude">The stop longitude.</param>
        /// <param name="latitude">The stop latitude.</param>
        /// <param name="attributes">The stop attributes.</param>
        /// <returns>An internal id representing the stop in this transit db.</returns>
        public static (uint tileId, uint localId) Add(this StopsDb stopsDb, string globalId, double longitude,
            double latitude, params Attribute[] attributes)
        {
            return stopsDb.Add(globalId, longitude, latitude, attributes);
        }


        public static IEnumerable<IStop> LocationsInRange(
            this StopsDb stopsDb, IStop stop, float maxDistance)
        {
            var l = new List<(uint, uint)>();
            var lat = (float) stop.Latitude;
            var lon = (float) stop.Longitude;
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