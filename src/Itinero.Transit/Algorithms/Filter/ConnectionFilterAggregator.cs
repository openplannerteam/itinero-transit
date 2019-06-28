using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;

namespace Itinero.Transit.Journey.Filter
{
    public class ConnectionFilterAggregator : IConnectionFilter
    {
        private List<IConnectionFilter> filters;

        public static IConnectionFilter CreateFrom(IConnectionFilter a, IConnectionFilter b)
        {
            return CreateFrom(new List<IConnectionFilter> {a, b});
        }

        public static IConnectionFilter CreateFrom(List<IConnectionFilter> filters)
        {
            filters = filters?.Where(v => v != null).ToList();
            if (filters == null || filters.Count == 0)
            {
                return null;
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (filters.Count == 1)
            {
                return filters[0];
            }

            return new ConnectionFilterAggregator(filters);
        }

        private ConnectionFilterAggregator(List<IConnectionFilter> filters)
        {
            this.filters = filters;
        }

        public bool CanBeTaken(IConnection c)
        {
            foreach (var filter in filters)
            {
                if (!filter.CanBeTaken(c))
                {
                    return false;
                }
            }

            return true;
        }

        public void CheckWindow(ulong depTime, ulong arrTime)
        {
            foreach (var filter in filters)
            {
                filter.CheckWindow(depTime, arrTime);
            }
        }
    }
}