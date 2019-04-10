using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Transit.Algorithms.Search;
using Itinero.Transit.Data.Walks;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]
namespace Itinero.Transit.Data
{
    public interface IStopsReader : IStop
    {
        bool MoveTo(LocationId stop);
        bool MoveTo(string globalId);
        void Reset();

        List<IStopsReader> UnderlyingDatabases { get; }

        /// <summary>
        /// Gives the internal StopsDb.
        /// Escapes the abstraction, should only be used for internal operations
        /// </summary>
        /// <returns></returns>
        StopsDb StopsDb { get; }
    }

    
    
    public static class StopsReaderExtensions
    {
        public static List<IStopsReader> FlattenedUnderlyingDatabases(this IStopsReader stopsReader)
        {
            if (stopsReader.UnderlyingDatabases == null)
            {
                return new List<IStopsReader> {stopsReader};
            }


            var list = new List<IStopsReader>();
            list.AddUnderlyingFlattened(stopsReader);
            return list;
        }

        private static void AddUnderlyingFlattened(this List<IStopsReader> flattened, IStopsReader stopsReader)
        {
            foreach (var underlying in stopsReader.UnderlyingDatabases)
            {
                if (underlying.UnderlyingDatabases == null)
                {
                    flattened.Add(underlying);
                }
                else
                {
                    flattened.AddUnderlyingFlattened(underlying);
                }
            }
        }

        public static LocationId FindStop(this IStopsReader reader, string locationId,
            string errorMessage = null)
        {
            if (!reader.MoveTo(locationId))
            {
                errorMessage = errorMessage ?? $"Departure location {locationId} was not found";
                throw new KeyNotFoundException(errorMessage);
            }

            return reader.Id;
        }

        public static IEnumerable<IStop> LocationsInRange(
            this IStopsReader stopsDb, IStop stop, float maxDistance)
        {
            var lat = (float) stop.Latitude;
            var lon = (float) stop.Longitude;
            return stopsDb.LocationsInRange(lat, lon, maxDistance);
        }

        public static IEnumerable<IStop> LocationsInRange(
            this IStopsReader stopsDb, float lat, float lon, float maxDistance)
        {
            if (maxDistance <= 0.1)
            {
                throw new ArgumentException("Oops, distance is zero or very small");
            }

            if (float.IsNaN(maxDistance) || float.IsInfinity(maxDistance) ||
                float.IsNaN(lat) || float.IsInfinity(lat) ||
                float.IsNaN(lon) || float.IsInfinity(lon)
            )
            {
                throw new ArgumentException(
                    "Oops, either lat, lon or maxDistance are invalid (such as NaN or Infinite)");
            }

            var box = (
                DistanceEstimate.MoveEast(lat, lon, -maxDistance), // minLon
                DistanceEstimate.MoveNorth(lat, lon, +maxDistance), // MinLat
                DistanceEstimate.MoveEast(lat, lon, +maxDistance), // MaxLon
                DistanceEstimate.MoveNorth(lat, lon, -maxDistance) //maxLat
            );

            return stopsDb.SearchInBox(box);
        }


        public static float CalculateDistanceBetween
            (this IStopsReader reader, LocationId departureLocation, LocationId targetLocation)
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