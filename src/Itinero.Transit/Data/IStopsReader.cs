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
        IEnumerable<IStop> SearchInBox((double minLon, double minLat, double maxLon, double maxLat) box);
    }


    public static class StopsReaderExtensions
    {
        //    IStop SearchClosest(double lon, double lat, double maxDistanceInMeters = 1000);


        /// <summary>
        /// Used by 'OtherModeExtensions' to search close by things for TimesBetween
        /// </summary>
        public static IEnumerable<IStop> LocationsInRange(
            this IStopsReader stopsDb, double lat, double lon, double maxDistance)
        {
            if (maxDistance <= 0.1)
            {
                throw new ArgumentException("Oops, distance is zero or very small");
            }

            if (double.IsNaN(maxDistance) || double.IsInfinity(maxDistance) ||
                double.IsNaN(lat) || double.IsInfinity(lat) ||
                double.IsNaN(lon) || double.IsInfinity(lon) ||
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                lat == double.MaxValue ||
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                lon == double.MaxValue
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

            if (double.IsNaN(box.Item1) ||
                double.IsNaN(box.Item2) ||
                double.IsNaN(box.Item3) ||
                double.IsNaN(box.Item4) ||
                box.Item1 > 180 || box.Item1 < -180 ||
                box.Item3 > 180 || box.Item3 < -180 ||
                box.Item2 > 90 || box.Item2 < -90 ||
                box.Item4 > 90 || box.Item4 < -90
            )
            {
                throw new Exception("Bounding box has NaN or is out of range");
            }

            return stopsDb.SearchInBox(box);
        }

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
            double latitude, double longitude, double maxDistanceInMeters = 1000.0)
        {
            StopId? closestStopId = null;
            var d = double.MaxValue;
            foreach (var stop in reader.LocationsInRange(latitude, longitude, maxDistanceInMeters))
            {
                var ds = DistanceEstimate.DistanceEstimateInMeter(
                    stop.Latitude, stop.Longitude, latitude, longitude);
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

        public static IStopsReader UseCache(this IStopsReader stopsReader)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (stopsReader is StopSearchCaching)
            {
                return stopsReader;
            }

            return new StopSearchCaching(stopsReader);
        }
    }
}