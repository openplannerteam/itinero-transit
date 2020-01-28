using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data.Aggregators
{
    public class TripsDbAggregator : ITripsDb
    {
        private IDatabaseReader<TripId, Trip> _aggregator;
        private readonly List<ITripsDb> _fallbacks;

        public static ITripsDb CreateFrom(List<ITripsDb> fallbacks)
        {
            if (fallbacks.Count == 1)
            {
                return fallbacks[0];
            }
            return new TripsDbAggregator(fallbacks);
        }

        public long Count
        {
            get
            {
                return _fallbacks.Sum(fallback => fallback.Count);
            }
        }
        
        public static ITripsDb CreateFrom(IEnumerable<TransitDbSnapShot> snapshots)
        {
            var dbs = snapshots.Select(sn => sn.TripsDb).ToList();
            return CreateFrom(dbs);
        }
        
        private TripsDbAggregator(List<ITripsDb> fallbacks)
        {
            _fallbacks = fallbacks;
            _aggregator = DatabaseAggregator<TripId, Trip>.CreateFrom(
                fallbacks.Select(db => (IDatabaseReader<TripId, Trip>) db).ToList());
        }

        public IEnumerator<Trip> GetEnumerator()
        {
            return _aggregator.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _aggregator).GetEnumerator();
        }

        public bool TryGet(TripId id, out Trip t)
        {
            return _aggregator.TryGet(id, out t);
        }

        public bool TryGetId(string globalId, out TripId id)
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

        public ITripsDb Clone()
        {
            return new TripsDbAggregator(_fallbacks);
        }
    }
}