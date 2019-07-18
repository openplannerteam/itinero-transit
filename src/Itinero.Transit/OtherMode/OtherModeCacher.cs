using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Logging;
using Itinero.Transit.Utils;

namespace Itinero.Transit.OtherMode
{
    public class OtherModeCacher : IOtherModeGenerator
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public IOtherModeGenerator Fallback { get; }

        public OtherModeCacher(IOtherModeGenerator fallback)
        {
            Fallback = fallback;
        }


        private readonly Dictionary<(StopId, StopId tos), uint> _cacheSingle =
            new Dictionary<(StopId, StopId tos), uint>();

        public uint TimeBetween(IStop from, IStop to)
        {
            var key = (from.Id, to.Id);
            if (_cacheSingle.ContainsKey(key))
            {
                return _cacheSingle[key];
            }

            var v = Fallback.TimeBetween(@from, to);
            _cacheSingle[key] = v;
            return v;
        }


        private readonly Dictionary<(StopId Id, KeyList<StopId> tos), Dictionary<StopId, uint>> _cache =
            new Dictionary<(StopId @from, KeyList<StopId> tos), Dictionary<StopId, uint>>();

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
            if (_cache.ContainsKey(key))
            {
                return _cache[key];
            }

            var v = Fallback.TimesBetween(@from, to);
            if (!_cacheIsClosed)
            {
                _cache[key] = v;
            }

            return v;
        }
        private readonly Dictionary<(StopId Id, KeyList<StopId> tos), Dictionary<StopId, uint>> _cacheReverse =
            new Dictionary<(StopId @from, KeyList<StopId> tos), Dictionary<StopId, uint>>();

        
        public Dictionary<StopId, uint> TimesBetween(IEnumerable<IStop> @from, IStop to)
        {
            from = from.Select(stop => new Stop(stop)).ToList();
            var froms = new KeyList<StopId>(from.Select(stop => stop.Id));
            var key = (to.Id, froms);
            if (_cacheReverse.ContainsKey(key))
            {
                return _cacheReverse[key];
            }

            var v = Fallback.TimesBetween(@from, to);
            if (!_cacheIsClosed)
            {
                _cacheReverse[key] = v;
            }

            return v;
        }

        private bool _cacheIsClosed;

        // ReSharper disable once UnusedMember.Global
        public OtherModeCacher PreCalculateCache(IStopsReader withCache)
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

            if (!(withCache is StopSearchCaching))
            {
                throw new Exception("You'll really want to use a caching stops reader here!");
            }

            var c = 0; // withCache.Count();
            var done = 0;
            while (withCache.MoveNext())
            {
                var start = DateTime.Now;
                var current = (IStop) withCache;
                Log.Information($"Searching around {current.GlobalId}");
                done++;
                var inRange = withCache.LocationsInRange(
                    current.Latitude, current.Longitude,
                    Range());
                TimesBetween(withCache, inRange);

                for (var i = 0; i < skiprate; i++)
                {
                    withCache.MoveNext();
                }

                var end = DateTime.Now;
                Log.Information($"Filling cache: {done}/{c} took {(end - start).TotalMilliseconds}ms");

            }
        }

        public float Range()
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