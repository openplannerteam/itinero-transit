using System;
using System.Collections.Generic;

namespace Itinero.Transit
{
    public class MemoizingTransferGenerator : IFootpathTransferGenerator
    {
        private readonly Dictionary<int, IContinuousConnection> _memoizationCache
            = new Dictionary<int, IContinuousConnection>();

        private readonly IFootpathTransferGenerator _fallback;

        public MemoizingTransferGenerator(IFootpathTransferGenerator fallback)
        {
            _fallback = fallback;
        }


        public IContinuousConnection GenerateFootPaths(DateTime departureTime, Location from, Location to)
        {
            var key = from.Uri.GetHashCode() + to.Uri.GetHashCode();
            IContinuousConnection conn;
            if (_memoizationCache.ContainsKey(key))
            {
                conn = _memoizationCache[key];
            }
            else
            {
                conn = _fallback.GenerateFootPaths(DateTime.MinValue, from, to);
                _memoizationCache[key] = conn;
            }

            return conn?.MoveDepartureTime(departureTime);
        }
    }
}