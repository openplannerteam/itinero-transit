using System.Collections.Generic;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.Data.Core;
// ReSharper disable InconsistentlySynchronizedField

namespace Itinero.Transit.Data.Aggregators
{
    public class StopSearchCache : IStopsReader
    {
        /// <summary>
        /// The fallback stops reader
        /// </summary>
        private readonly IStopsReader _fallback;

        /// <summary>
        /// The cache of what stops are in range of the given stop
        /// {(Stop, range to search) --> these stops are closeby}
        /// </summary>
        private readonly Dictionary<(Stop, uint), HashSet<Stop>> _stopsCache;

        /// <summary>
        /// IF the cache is closed, NO NEW VALUES will be calculated.
        /// Instead, the cached values will be returned
        /// </summary>
        private bool _cacheIsClosed;

        public StopSearchCache(IStopsReader stopsReader)
        {
            _stopsCache = new Dictionary<(Stop, uint), HashSet<Stop>>();
            _fallback = stopsReader;
        }

        // ReSharper disable once UnusedMember.Global
        public StopSearchCache(IStopsReader stopsReader, StopSearchCache shareCacheWith)
        {
            _fallback = stopsReader;
            _stopsCache = shareCacheWith._stopsCache;
        }


        public IEnumerable<Stop> StopsAround(Stop stop, uint range)
        {
            var key = (stop, range);
            if (_stopsCache.ContainsKey(key))
            {
                return _stopsCache[key];
            }

            if (_cacheIsClosed)
            {
                return _fallback.StopsAround(stop, range);
            }

            var set = new HashSet<Stop>();
            foreach (var s in _fallback.StopsAround(stop, range))
            {
                set.Add(new Stop(s));
            }

            lock (_stopsCache)
            {
                _stopsCache[key] = set;
            }

            return set;
        }

        /// <summary>
        /// When the cache is closed, NO NEW VALUES will be cached.
        /// If a request is not in the cache, it will be passed to the fallback provider.
        ///
        ///
        /// This is meant for multi-level caches:
        ///
        /// There is one long-living cache A, which keeps track of data 99% of the users will need
        /// There is one short-living cache B which is only useful for one single request (but will often be needed during the request).
        /// Then, B will use A as fallback.
        /// The needed values for B are precomputed and B is closed.
        /// B can then be passed into the requesting algorithm.
        /// The user-specific requests (which were precomputed) will be answered by B, whereas the rest will be answered by A
        ///
        /// 
        /// </summary>
        public void CloseCache()
        {
            _cacheIsClosed = true;
        }

        
        /// <summary>
        /// Should ONLY be used for preprocessing OSM -> PT-stops
        ///
        /// </summary>
        public void MakeComplete()
        {
            lock (_stopsCache)
            {
                CloseCache();
                var toAdd = new Dictionary<(Stop, uint), HashSet<Stop>>();
                foreach (var inRange in _stopsCache)
                {
                    // This only concerns 'crows flight' in range
                    // so if A is in range of B, B is also in range of A
                    var a = inRange.Key.Item1;
                    var range = inRange.Key.Item2;
                    var bs = inRange.Value;

                    foreach (var b in bs)
                    {
                        var key = (b, range);
                        if (!toAdd.ContainsKey(key))
                        {
                            toAdd[key] = new HashSet<Stop>
                            {
                                a
                            };
                        }
                        else
                        {
                            toAdd[key].Add(a);
                        }
                    }
                }

                foreach (var kv in toAdd)
                {
                    if (_stopsCache.ContainsKey(kv.Key))
                    {
                        _stopsCache[kv.Key].UnionWith(kv.Value);
                    }
                    else
                    {
                        _stopsCache[kv.Key] = kv.Value;
                    }
                }
            }
        }

        public uint CacheCount()
        {
            return (uint) _stopsCache.Count;
        }

        // ----------- Only boring, generated code below ------------ //        


        public string GlobalId => _fallback.GlobalId;

        public StopId Id => _fallback.Id;

        public double Longitude => _fallback.Longitude;

        public double Latitude => _fallback.Latitude;

        public IAttributeCollection Attributes => _fallback.Attributes;

        public bool MoveTo(StopId stop)
        {
            return _fallback.MoveTo(stop);
        }

        public bool MoveTo(string globalId)
        {
            return _fallback.MoveTo(globalId);
        }

        public HashSet<uint> DatabaseIndexes()
        {
            return _fallback.DatabaseIndexes();
        }

        public bool MoveNext()
        {
            return _fallback.MoveNext();
        }

        public void Reset()
        {
            _fallback.Reset();
        }
    }
}