using System.Collections;
using System.Collections.Generic;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.LocationIndexing;

namespace Itinero.Transit.Data.Aggregators
{
    public class StopsDbCache : IStopsDb
    {
        private readonly IStopsDb _fallback;

        public StopsDbCache(IStopsDb stopsDbImplementation)
        {
            _fallback = stopsDbImplementation;
        }
        
        private Dictionary<(double lon, double lat, uint maxDistance), List<Stop>> _cache = new Dictionary<(double lon, double lat, uint maxDistance), List<Stop>>();
        
        public List<Stop> GetInRange((double lon, double lat) c, uint maxDistanceInMeter)
        {
            var key = (c.lon, c.lat, maxDistanceInMeter);
            if (_cache.TryGetValue(key, out var data))
            {
                return data;
            }
            
            var fresh = _fallback.GetInRange(c, maxDistanceInMeter);
            _cache[key] = fresh;
            return fresh;
        }

        public IEnumerator<Stop> GetEnumerator()
        {
            return _fallback.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _fallback).GetEnumerator();
        }

        public bool TryGet(StopId id, out Stop t)
        {
            return _fallback.TryGet(id, out t);
        }

        public bool TryGetId(string globalId, out StopId id)
        {
            return _fallback.TryGetId(globalId, out id);
        }

        public IEnumerable<uint> DatabaseIds => _fallback.DatabaseIds;



        public IStopsDb Clone()
        {
            return _fallback.Clone();
        }

        public ILocationIndexing<Stop> LocationIndex => _fallback.LocationIndex;



        public void PostProcess(uint zoomlevel)
        {
            _fallback.PostProcess(zoomlevel);
        }
    }

    public static class StopsDbExtensions
    {
        public static StopsDbCache UseCache(this IStopsDb stopsDb)
        {
            if (stopsDb is StopsDbCache cached)
            {
                return cached;
            }
            return new StopsDbCache(stopsDb);
        }
    }
}