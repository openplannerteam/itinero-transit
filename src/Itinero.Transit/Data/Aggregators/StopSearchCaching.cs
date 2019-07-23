using System.Collections.Generic;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data.Aggregators
{
    public class StopSearchCaching : IStopsReader
    {
        private readonly IStopsReader _stopsReader;

        private readonly Dictionary<(Stop, uint), HashSet<Stop>> _stopsCache;

        private readonly IEnumerable<Stop> _empty = new Stop[0];

        /// <summary>
        /// IF the cache is closed, NO NEW VALUES will be calculated.
        /// Instead, the cached values will be returned
        /// </summary>
        private bool _cacheIsClosed;

        public StopSearchCaching(IStopsReader stopsReader)
        {
            _stopsCache = new Dictionary<(Stop, uint), HashSet<Stop>>();
            _stopsReader = stopsReader;
        }

        // ReSharper disable once UnusedMember.Global
        public StopSearchCaching(IStopsReader stopsReader, StopSearchCaching shareCacheWith)
        {
            _stopsReader = stopsReader;
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
                return _empty;
            }

            var set = new HashSet<Stop>();
            foreach (var s in _stopsReader.StopsAround(stop, range))
            {
                set.Add(new Stop(s));
            }

            _stopsCache[key] = set;
            return set;
        }

        public void MakeComplete()
        {
            _cacheIsClosed = true;
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
                _stopsCache[kv.Key] = kv.Value;
            }
        }

        public uint CacheCount()
        {
            return (uint) _stopsCache.Count;
        }

        // ----------- Only boring, generated code below ------------ //        


        public string GlobalId => _stopsReader.GlobalId;

        public StopId Id => _stopsReader.Id;

        public double Longitude => _stopsReader.Longitude;

        public double Latitude => _stopsReader.Latitude;

        public IAttributeCollection Attributes => _stopsReader.Attributes;

        public bool MoveTo(StopId stop)
        {
            return _stopsReader.MoveTo(stop);
        }

        public bool MoveTo(string globalId)
        {
            return _stopsReader.MoveTo(globalId);
        }

        public HashSet<uint> DatabaseIndexes()
        {
            return _stopsReader.DatabaseIndexes();
        }

        public bool MoveNext()
        {
            return _stopsReader.MoveNext();
        }

        public void Reset()
        {
            _stopsReader.Reset();
        }
    }
}