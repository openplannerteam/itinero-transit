using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]

namespace Itinero.Transit.Data
{
    public interface IStopsReader : IStop
    {
        bool MoveNext();
        bool MoveTo(LocationId stop);
        bool MoveTo(string globalId);
        void Reset();


        /// <summary>
        /// Calculates the walking distance between two stops.
        /// Might _not_ be symmetrical due to oneways
        /// </summary>
        /// <param name="departureLocation"></param>
        /// <param name="targetLocation"></param>
        /// <returns></returns>
        float CalculateDistanceBetween(LocationId departureLocation, LocationId targetLocation);
        IEnumerable<IStop> LocationsInRange(double lat, double lon, double range);

        IEnumerable<IStop> SearchInBox((double minLon, double minLat, double maxLon, double maxLat) box);
        IStop SearchClosest(double lon, double lat, double maxDistanceInMeters = 1000);


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
    }
}