using System.Collections.Generic;
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
        public List<T> GetInRange((double lon, double lat) c, double maxDistanceInMeter)
        {
            return _fallbacks.SelectMany(fallback => fallback.GetInRange(c, maxDistanceInMeter)).ToList();
        }
    }
}