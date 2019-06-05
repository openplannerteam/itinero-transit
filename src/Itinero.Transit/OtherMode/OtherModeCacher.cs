using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;

namespace Itinero.Transit.OtherMode
{
    public class OtherModeCacher : IOtherModeGenerator
    {
        private IOtherModeGenerator _fallback;

        public OtherModeCacher(IOtherModeGenerator fallback)
        {
            _fallback = fallback;
        }


        private Dictionary<(LocationId from, LocationId tos), uint> _cacheSingle =
            new Dictionary<(LocationId @from, LocationId tos), uint>();

        public uint TimeBetween(IStopsReader reader, LocationId @from, LocationId to)
        {
            var key = (from, to);
            if (_cacheSingle.ContainsKey(key))
            {
                return _cacheSingle[key];
            }

            var v = _fallback.TimeBetween(reader, @from, to);
            _cacheSingle[key] = v;
            return v;
        }


        private Dictionary<(LocationId from, List<LocationId> tos), Dictionary<LocationId, uint>> _cache =
            new Dictionary<(LocationId @from, List<LocationId> tos), Dictionary<LocationId, uint>>();

        public Dictionary<LocationId, uint> TimesBetween(IStopsReader reader, LocationId @from,
            IEnumerable<LocationId> to)
        {
            var tos = to.ToList();

            var key = (from, tos);
            if (_cache.ContainsKey(key))
            {
                return _cache[key];
            }

            var v = _fallback.TimesBetween(reader, @from, tos);
            _cache[key] = v;
            return v;
        }

        public float Range()
        {
            return _fallback.Range();
        }
    }
}