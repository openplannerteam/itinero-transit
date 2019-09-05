using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Utils;
// ReSharper disable InconsistentlySynchronizedField

namespace Itinero.Transit.OtherMode
{
    public class OtherModeCache : IOtherModeGenerator
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public IOtherModeGenerator Fallback { get; }

        public OtherModeCache(IOtherModeGenerator fallback)
        {
            Fallback = fallback;
        }


        /// <summary>
        /// Keeps track of single instances: from A to B: how long does it take (or MaxValue if not possible)
        /// </summary>
        private readonly Dictionary<(StopId, StopId tos), uint> _cacheSingle =
            new Dictionary<(StopId, StopId tos), uint>();

        /// <summary>
        /// Keeps track of how long it takes to go from A to multiple B's
        /// </summary>
        private readonly Dictionary<(StopId Id, KeyList<StopId> tos), Dictionary<StopId, uint>> _cacheForward =
            new Dictionary<(StopId @from, KeyList<StopId> tos), Dictionary<StopId, uint>>();


        /// <summary>
        /// Keeps track of how long it takes to go from multiple As to one single locations
        /// This makes sense to do: the access pattern will often need the same closeby stops
        /// </summary>
        private readonly Dictionary<(KeyList<StopId> froms, StopId to), Dictionary<StopId, uint>> _cacheReverse =
            new Dictionary<(KeyList<StopId> froms, StopId to), Dictionary<StopId, uint>>();


        public uint TimeBetween(IStop from, IStop to)
        {
            var key = (from.Id, to.Id);
            // ReSharper disable once InconsistentlySynchronizedField
            if (_cacheSingle.ContainsKey(key))
            {
                // ReSharper disable once InconsistentlySynchronizedField
                return _cacheSingle[key];
            }

            var v = Fallback.TimeBetween(@from, to);
            lock (_cacheSingle)
            {
                _cacheSingle[key] = v;
            }

            return v;
        }


        public Dictionary<StopId, uint> TimesBetween(IStop from,
            IEnumerable<IStop> to)
        {
            /**
             * Tricky situation ahead...
             *
             * The 'to'-list of IStops is probably generated with a return yield ala:
             * 
             *
             * var potentialStopsInRange = ...
             * for(var stop in potentialStopsInRange){
             *    if(distanceBetween(stop, target) <= range{
             *         reader.MoveTo(stop);
             *        yield return stop;
             *     }
             * }
             *
             * (e.g. SearchInBox, which uses 'yield return stopSearchEnumerator.Current')
             *
             * In other words, something as ToList would result in:
             * [reader, reader, reader, ... , reader], which points internally to the same stop
             *
             * But we want all the ids to be able to cache.
             * And we cant use the 'to'-list as cache key, because it points towards the same reader n-times.
             *
             */


            // FIRST select the IDs to make sure 'return yield'-enumerators run correctly on an enumerating object
            to = to.Select(stop => new Stop(stop)).ToList();
            var tos = new KeyList<StopId>(to.Select(stop => stop.Id));
            var key = (from.Id, tos);
            if (_cacheForward.ContainsKey(key))
            {
                return _cacheForward[key];
            }

            // The end result... Empty for now
            var v = new Dictionary<StopId, uint>();

            // What do we _actually_ have to search. A few values might be available already
            var toSearch = new List<IStop>();

            foreach (var t in to)
            {
                var keySingle = (from.Id, t.Id);
                if (_cacheSingle.ContainsKey(keySingle))
                {
                    // Found!
                    v.Add(t.Id, _cacheSingle[keySingle]);
                }
                else if (!from.Id.Equals(t.Id))
                {
                    // This one should still be searched

                    toSearch.Add(t);
                }
            }

            if (toSearch.Count != 0)
            {
                var rawSearch = Fallback.TimesBetween(@from, to);
                foreach (var found in rawSearch)
                {
                    v[found.Key] = found.Value;
                }

                // Add those individual searches to the _cacheSingle as well
                if (!_cacheIsClosed)
                {
                    lock (_cacheSingle)
                    {
                        foreach (var t in to)
                        {
                            if (v.ContainsKey(t.Id))
                            {
                                _cacheSingle[(from.Id, t.Id)] = v[t.Id];
                            }
                            else
                            {
                                _cacheSingle[(from.Id, t.Id)] = uint.MaxValue;
                            }
                        }
                    }
                }
            }


            // ReSharper disable once InvertIf
            if (!_cacheIsClosed)
            {
                lock (_cacheForward)
                {
                    _cacheForward[key] = v;
                }
            }

            return v;
        }


        public Dictionary<StopId, uint> TimesBetween(IEnumerable<IStop> @fromEnum, IStop to)
        {
            var from = fromEnum.Select(stop => new Stop(stop)).ToList();
            var froms = new KeyList<StopId>(from.Select(stop => stop.Id));
            var key = (froms, to.Id);

            // Already found in the cache
            if (_cacheReverse.ContainsKey(key))
            {
                return _cacheReverse[key];
            }

            // The end result... Empty for now
            var v = new Dictionary<StopId, uint>();


            // What do we _actually_ have to search. A few values might be available already
            var toSearch = new List<Stop>();
            foreach (var f in from)
            {
                var keySingle = (f.Id, to.Id);
                if (_cacheSingle.ContainsKey(keySingle))
                {
                    // This value already exists
                    v.Add(f.Id, _cacheSingle[keySingle]);
                }
                else if (!f.Id.Equals(to.Id))
                {
                    // This one should still be searched
                    toSearch.Add(f);
                }
            }


            if (toSearch.Count != 0)
            {
                // There are still values to search for. Lets do that now
                var rawSearch = Fallback.TimesBetween(@toSearch, to);
                foreach (var found in rawSearch)
                {
                    v[found.Key] = found.Value;
                }

                // Add those individual searches to the _cacheSingle as well
                if (!_cacheIsClosed)
                {
                    lock (_cacheSingle)
                    {
                        foreach (var fr in from)
                        {
                            if (v.ContainsKey(fr.Id))
                            {
                                _cacheSingle[(fr.Id, to.Id)] = v[fr.Id];
                            }
                            else
                            {
                                _cacheSingle[(fr.Id, to.Id)] = uint.MaxValue;
                            }
                        }
                    }
                }
            }


            // And add to the final cache
            // ReSharper disable once InvertIf
            if (!_cacheIsClosed)
            {
                lock (_cacheReverse)
                {
                    _cacheReverse[key] = v;
                }
            }

            return v;
        }


        private bool _cacheIsClosed;

        public void PreCalculateCache(IStopsReader withCache)
        {
            if (Range() == 0)
            {
                throw new Exception("Range is 0. It has no use to precalculate a cache of stops within 0 meter of other stops...");
            }
            withCache.Reset();
            if (!(withCache is StopSearchCache))
            {
                throw new Exception("You'll really want to use a caching stops reader here!");
            }

            while (withCache.MoveNext())
            {
                var current = (IStop) withCache;
                var inRange = withCache.StopsAround(new Stop(current), Range());
                TimesBetween(withCache, inRange);
            }
        }

        public uint Range()
        {
            return Fallback.Range();
        }

        public string OtherModeIdentifier()
        {
            return Fallback.OtherModeIdentifier();
        }

        public IOtherModeGenerator GetSource(StopId @from, StopId to)
        {
            return Fallback.GetSource(from, to);
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
        /// </summary>
        public void CloseCache()
        {
            _cacheIsClosed = true;
        }
    }
}