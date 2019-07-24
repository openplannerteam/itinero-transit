using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Utils;

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

        // ReSharper disable once UnusedMember.Global
        public OtherModeCache PreCalculateCache(IStopsReader withCache)
        {
            // ReSharper disable once RedundantArgumentDefaultValue
            PreCalculateCache(withCache, 0, 0);
            return this;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public void PreCalculateCache(IStopsReader withCache, int offset, int skiprate = 0)
        {
            withCache.Reset();

            for (var i = 0; i < offset; i++)
            {
                withCache.MoveNext();
            }

            if (!(withCache is StopSearchCache))
            {
                throw new Exception("You'll really want to use a caching stops reader here!");
            }

            var c = 0; // withCache.Count();
            var done = 0;
            while (withCache.MoveNext())
            {
                var current = (IStop) withCache;
                done++;
                var inRange = withCache.StopsAround(new Stop(current), Range());
                TimesBetween(withCache, inRange);

                for (var i = 0; i < skiprate; i++)
                {
                    withCache.MoveNext();
                }
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
        /// Consider the following situation: you have a big cache of all the timings between all the public transport stops.
        /// You need to add in a few extra floating points, which you'll only shortly need.
        ///
        /// For this, you can create a small, extra cache, add the needed stuff and let the others delegate to the fallback
        /// 
        /// </summary>
        public void CloseCache()
        {
            _cacheIsClosed = true;
        }
    }
}