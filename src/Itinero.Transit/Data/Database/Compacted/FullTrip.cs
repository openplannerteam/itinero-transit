using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data.Compacted
{
    public struct FullTrip : IGlobalId, IEnumerable<Connection>
    {
        public string GlobalId { get; }
        public IReadOnlyDictionary<string, string> Attributes { get; }


        public readonly ulong FirstDeparture;
        public readonly TimeSchedule TimeSchedule; // or ID?
        public readonly Route Route;
        public readonly TripId Trip;
        public readonly string ConnectionPrefix;

        public FullTrip(string globalId,
            ulong firstDeparture, TimeSchedule timeSchedule, Route routeToFollow, TripId trip, string connectionPrefix,
            IReadOnlyDictionary<string, string> attributes = null)
        {
            FirstDeparture = firstDeparture;
            TimeSchedule = timeSchedule;
            Route = routeToFollow;
            Trip = trip;
            ConnectionPrefix = connectionPrefix;
            GlobalId = globalId;
            Attributes = attributes ?? new Dictionary<string, string>();
        }

        [Pure]
        public Connection GenerateConnection(int index)
        {
            return new Connection(ConnectionPrefix + index,
                Route[index],
                Route[index + 1],
                FirstDeparture + TimeSchedule[index * 2],
                (ushort) (TimeSchedule[index * 2 + 1] - TimeSchedule[index * 2]),
                Trip
            );
        }

        [Pure]
        public IEnumerator<Connection> GetEnumerator()
        {
            return new SimpleConnectionEnumerator(this);
        }

        [Pure]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class SimpleConnectionEnumerator : IEnumerator<Connection>
    {
        private readonly FullTrip _fullTrip;
        private int _currentIndex = -1;

        public SimpleConnectionEnumerator(FullTrip fullTrip)
        {
            _fullTrip = fullTrip;
        }

        public bool MoveNext()
        {
            _currentIndex++;
            Current = _fullTrip.GenerateConnection(_currentIndex);
            return _currentIndex < _fullTrip.Route.Count;
        }

        public void Reset()
        {
            _currentIndex = -1;
        }

        public Connection Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }

    internal class FullTripComparer : Comparer<FullTrip>
    {
        public override int Compare(FullTrip x, FullTrip y)
        {
            return (int) ((long) x.FirstDeparture - (long) y.FirstDeparture);
        }
    }
}