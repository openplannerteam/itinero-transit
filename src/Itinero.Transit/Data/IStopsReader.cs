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

    }


    public static class StopsReaderExtensions
    {

        public static LocationId FindStop(this IStopsReader reader, string locationId,
            string errorMessage = null)
        {
            // ReSharper disable once InvertIf
            if (!reader.MoveTo(locationId))
            {
                errorMessage = errorMessage ?? $"Departure location {locationId} was not found";
                throw new KeyNotFoundException(errorMessage);
            }

            return reader.Id;
        }
    }
}