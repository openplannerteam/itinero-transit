using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.LocationIndexing;

namespace Itinero.Transit.Data.Aggregators
{
    public class StopsDbAggregator : IStopsDb
    {
        private readonly IDatabaseReader<StopId, Stop> _data;
        private IReadOnlyCollection<IStopsDb> _fallbacks;
        public ILocationIndexing<Stop> LocationIndex { get; }


        public StopsDbAggregator(IReadOnlyCollection<IStopsDb> fallbacks)
        {
            _fallbacks = fallbacks;
            _data = DatabaseAggregator<StopId, Stop>.CreateFrom(
                fallbacks.Select(fb => (IDatabaseReader<StopId, Stop>) fb).ToList());

            LocationIndex = new LocationIndexAggregator<Stop>(
                fallbacks.Select(fb => fb.LocationIndex).ToList());
        }


        public IEnumerator<Stop> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _data).GetEnumerator();
        }

        public bool TryGet(StopId id, out Stop t)
        {
            return _data.TryGet(id, out t);
        }

        public bool TryGetId(string globalId, out StopId id)
        {
            return _data.TryGetId(globalId, out id);
        }

        public List<Stop> GetInRange((double lon, double lat) c, uint maxDistanceInMeter)
        {
            return LocationIndex.GetInRange(c, maxDistanceInMeter);
        }

        public IEnumerable<uint> DatabaseIds => _data.DatabaseIds;

        /// <summary>
        /// Makes a shallow clone
        /// </summary>
        /// <returns></returns>
        public IStopsDb Clone()
        {
            return new StopsDbAggregator(_fallbacks);
        }


        public void PostProcess(uint zoomlevel)
        {
            foreach (var fallback in _fallbacks)
            {
                fallback.PostProcess(zoomlevel);
            }
        }

        public static IStopsDb CreateFrom(IEnumerable<TransitDbSnapShot> snapshots)
        {
            var stopsDbs = snapshots.Select(sn => sn.Stops).ToList();
            return CreateFrom(stopsDbs);
        }

        public static IStopsDb CreateFrom(List<IStopsDb> tdbs)
        {
            if (tdbs.Count == 1)
            {
                return tdbs[0];
            }

            return new StopsDbAggregator(tdbs);
        }
    }
}