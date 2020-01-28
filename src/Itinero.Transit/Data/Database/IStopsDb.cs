using System.Collections.Generic;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.LocationIndexing;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Data
{
    public interface IStopsDb : IDatabaseReader<StopId, Stop>, IClone<IStopsDb>
    {
        ILocationIndexing<Stop> LocationIndex { get; }

        void PostProcess(uint zoomLevel = 12);
        
        long Count { get; }
    }

    public static class StopsDbExtensions
    {
        public static Stop FindClosest(this IStopsDb stops, Stop around, uint maxDistance)
        {
            return stops.FindClosest((around.Longitude, around.Latitude), maxDistance);
        }

        public static List<Stop> GetInRange(this IStopsDb db, (double lon, double lat) c, uint maxDistanceInMeter)
        {
            return db.LocationIndex.GetInRange(c, maxDistanceInMeter);
        }
        
        public static List<Stop> GetInRange(this IStopsDb db, Stop stop, uint maxDistanceInMeter)
        {
            return db.LocationIndex.GetInRange((stop.Longitude, stop.Latitude), maxDistanceInMeter);
        }


        // ReSharper disable once MemberCanBePrivate.Global
        public static Stop FindClosest(this IStopsDb stops, (double lon, double lat) c, uint maxDistanceInMeters)
        {
            var inRange = stops.GetInRange(c, maxDistanceInMeters);
            Stop closest = null;
            var minDistance = uint.MaxValue;
            foreach (var stop in inRange)
            {
                var d = DistanceEstimate.DistanceEstimateInMeter(c, (stop.Longitude, stop.Latitude));
                if (d < minDistance)
                {
                    minDistance = (uint) d;
                    closest = stop;
                }
            }

            return closest;
        }
    }
}