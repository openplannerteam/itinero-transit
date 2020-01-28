using System.Collections.Generic;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.Simple;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Data.Compacted
{
    /// <summary>
    /// A route is a collection of stops that are commonly travelled together
    /// </summary>
    public class SimpleRoutesDb : SimpleDb<RouteId, Route>, IRoutesDb
    {
        private int _lastLength;
        private Dictionary<KeyList<StopId>, RouteId> _routesByStops = new Dictionary<KeyList<StopId>, RouteId>();

        private RouteId _idFactory = new RouteId();

        public SimpleRoutesDb(uint dbId) : base(dbId)
        {
        }

        public SimpleRoutesDb(SimpleDb<RouteId, Route> copyFrom) : base(copyFrom)
        {
        }


        public void PostProcess()
        {
            for (var i = _lastLength; i < Data.Count; i++)
            {
                var route = Data[i];
                _routesByStops[route] = (RouteId) _idFactory.Create(DatabaseId, (ulong) i);
            }

            _lastLength = Data.Count;
        }

        public bool TryGetId(IEnumerable<StopId> stops, out RouteId id)
        {
            if (stops is KeyList<StopId> key)
            {
                return _routesByStops.TryGetValue(key, out id);
            }

            return _routesByStops.TryGetValue(new KeyList<StopId>(stops), out id);
        }
    }
}