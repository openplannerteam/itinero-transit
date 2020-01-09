using System;
using System.Collections;
using System.Collections.Generic;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data.Aggregators
{
    public class TripsDbAggregator : ITripsDb
    {
        private DatabaseAggregator<TripId, Trip> _aggregator;
        private readonly List<ITripsDb> _fallbacks;

        public TripsDbAggregator(List<ITripsDb> fallbacks)
        {
            _fallbacks = fallbacks;
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