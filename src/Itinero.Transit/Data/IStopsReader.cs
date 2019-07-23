using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Utils;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]

namespace Itinero.Transit.Data
{
    public interface IStopsReader : IStop
    {
        HashSet<uint> DatabaseIndexes();

        bool MoveNext();
        bool MoveTo(StopId stop);
        bool MoveTo(string globalId);
        void Reset();
        
        /// <summary>
        /// Searches locations around the given stop.
        /// The given stop itself should not be included
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        
        IEnumerable<Stop> StopsAround(Stop stop, uint range);

    }


    public static class StopsReaderExtensions
    {
        //    IStop SearchClosest(double lon, double lat, double maxDistanceInMeters = 1000);


        public static StopId FindStop(this IStopsReader reader, string locationId,
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

        public static Stop FindClosest(this IStopsReader reader,
            IStop around, uint maxDistanceInMeters = 1000)
        {
            StopId? closestStopId = null;
            var d = double.MaxValue;
            foreach (var stop in reader.StopsAround(new Stop(around), maxDistanceInMeters))
            {
                var ds = DistanceEstimate.DistanceEstimateInMeter(
                    stop.Latitude, stop.Longitude, around.Latitude, around.Longitude);
                if (ds < d)
                {
                    d = ds;
                    closestStopId = stop.Id;
                }
            }

            if (closestStopId == null)
            {
                return null;
            }

            reader.MoveTo(closestStopId.Value);
            return new Stop(reader);
        }

        public static IEnumerable<Stop> StopsAround(this IStopsReader reader, string globalId, uint range)
        {
            reader.MoveTo(globalId);
            return reader.StopsAround(new Stop(reader), range);
        }

        public static StopSearchCaching UseCache(this IStopsReader stopsReader)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (stopsReader is StopSearchCaching c)
            {
                return c;
            }

            return new StopSearchCaching(stopsReader);
        }
    }
}