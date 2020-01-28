using System.Collections.Generic;
using System.Runtime.InteropServices;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data.Compacted
{
    public interface IRoutesDb : IDatabaseReader<RouteId, Route>
    {
        void PostProcess();

        /// <summary>
        /// Gets the route ID based on the stops in the route
        /// </summary>
        /// <param name="stops"></param>
        bool TryGetId(IEnumerable<StopId> stops, out RouteId id);
    }
}