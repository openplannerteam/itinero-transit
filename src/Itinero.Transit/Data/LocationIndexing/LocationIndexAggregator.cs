using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Itinero.Transit.Data.LocationIndexing
{
    public class LocationIndexAggregator<T> : ILocationIndexing<T>
    {
        private readonly List<ILocationIndexing<T>> _fallbacks;

        public LocationIndexAggregator(List<ILocationIndexing<T>> fallbacks)
        {
            _fallbacks = fallbacks;
        }

        public IEnumerable<T> GetInBox((double minlon, double maxlat) nw, (double maxlon, double minlat) se)
        {
           return _fallbacks.SelectMany(fb => fb.GetInBox(nw, se));
        }

        [Pure]
        public List<T> GetInRange((double lat, double lon) c, uint maxDistanceInMeter)
        {
            return _fallbacks.SelectMany(fallback => fallback.GetInRange(c, maxDistanceInMeter)).ToList();
        }

        public (T, double distance) GetClosest((double lat, double lon) c, uint maxDistance)
        {
            T t = default(T);
            double distance = maxDistance;
            foreach (var fb in _fallbacks)
            {
                var (tfound, d) = fb.GetClosest(c, distance);
                if (d < distance)
                {
                    
                }
            }
        }
    }
}