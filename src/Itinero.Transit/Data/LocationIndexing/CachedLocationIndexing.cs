using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Itinero.Transit.Data.LocationIndexing
{
    public class CachedLocationIndexing<T> : ILocationIndexing<T>
    {
        private readonly ILocationIndexing<T> _fallback;

        public CachedLocationIndexing(ILocationIndexing<T> fallback)
        {
            _fallback = fallback;
        }

       
        private Dictionary<((double lat, double lon), uint maxDistance), List<T>> _cache =
            new Dictionary<((double lat, double lon), uint maxDistance), List<T>>();


        public IEnumerable<T> GetInBox((double minlon, double maxlat) nw, (double maxlon, double minlat) se)
        {
            return _fallback.GetInBox(nw, se);
        }

        [Pure]
        public List<T> GetInRange((double lon, double lat) c, double maxDistanceInMeter)
        {
            var key = (c, (uint) (maxDistanceInMeter+1));
            if (_cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var data = _fallback.GetInRange(c, maxDistanceInMeter);
            _cache[key] = data;
            return data;
        }
    }
}