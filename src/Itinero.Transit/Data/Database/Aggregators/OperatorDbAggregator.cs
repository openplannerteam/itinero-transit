using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data.Aggregators
{
    public class OperatorDbAggregator : IOperatorDb
    {
        private IDatabaseReader<OperatorId, Operator> _aggregator;
        private readonly List<IOperatorDb> _fallbacks;

        public static IOperatorDb CreateFrom(List<IOperatorDb> fallbacks)
        {
            if (fallbacks.Count == 1)
            {
                return fallbacks[0];
            }
            return new OperatorDbAggregator(fallbacks);
        }

        public long Count
        {
            get
            {
                return _fallbacks.Sum(fallback => fallback.Count);
            }
        }
        
        public static IOperatorDb CreateFrom(IEnumerable<TransitDbSnapShot> snapshots)
        {
            var dbs = snapshots.Select(sn => sn.OperatorDb).ToList();
            return CreateFrom(dbs);
        }
        
        private OperatorDbAggregator(List<IOperatorDb> fallbacks)
        {
            _fallbacks = fallbacks;
            _aggregator = DatabaseAggregator<OperatorId, Operator>.CreateFrom(
                fallbacks.Select(db => (IDatabaseReader<OperatorId, Operator>) db).ToList());
        }

        public IEnumerator<Operator> GetEnumerator()
        {
            return _aggregator.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _aggregator).GetEnumerator();
        }

        public bool TryGet(OperatorId id, out Operator t)
        {
            return _aggregator.TryGet(id, out t);
        }

        public bool TryGetId(string globalId, out OperatorId id)
        {
            return _aggregator.TryGetId(globalId, out id);
        }

        public IEnumerable<uint> DatabaseIds => _aggregator.DatabaseIds;
        public void PostProcess()
        {
            foreach (var fallback in _fallbacks)
            {
                fallback.PostProcess();
            }
        }

        public IOperatorDb Clone()
        {
            return new OperatorDbAggregator(_fallbacks);
        }
    }
}