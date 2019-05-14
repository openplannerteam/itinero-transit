using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Itinero.Transit.Data.Aggregators
{
    public class ConnectionEnumeratorAggregator : IConnectionEnumerator
    {
        private IConnectionEnumerator _currentConnection;

        private readonly IConnectionEnumerator _a, _b;
        private bool _aDepleted, _bDepleted;

        public static IConnectionEnumerator CreateFrom(IEnumerable<TransitDb.TransitDbSnapShot> enumerators)
        {
            var depEnumerators =
                enumerators.Select(tdb => (IConnectionEnumerator) tdb.ConnectionsDb.GetDepartureEnumerator()).ToList();
            return CreateFrom(depEnumerators);
        }

        public static IConnectionEnumerator CreateFrom(List<IConnectionEnumerator> enumerators)
        {
            if (enumerators.Count == 0)
            {
                throw new Exception("No enumerators found");
            }

            if (enumerators.Count == 1)
            {
                return enumerators[0];
            }

            return new ConnectionEnumeratorAggregator(enumerators);
        }

        private ConnectionEnumeratorAggregator(List<IConnectionEnumerator> enumerators)
        {
            switch (enumerators.Count)
            {
                case 0:
                case 1:
                    throw new ArgumentException("At least two enumerators are needed to fuse them");
                case 2:
                    _a = enumerators[0];
                    _b = enumerators[1];
                    break;
                case 3:
                    _a = enumerators[0];
                    _b = new ConnectionEnumeratorAggregator(enumerators[1], enumerators[2]);
                    break;
                default:
                    var halfway = enumerators.Count / 2;
                    _a = CreateFrom(enumerators.GetRange(0, halfway));
                    _b = CreateFrom(enumerators.GetRange(halfway, enumerators.Count - halfway));
                    break;
            }
        }

        private ConnectionEnumeratorAggregator(IConnectionEnumerator a, IConnectionEnumerator b)
        {
            _a = a;
            _b = b;
        }


        private void MoveNextA(DateTime? dateTime = null)
        {
            try
            {
                if (!_a.MoveNext(dateTime))
                {
                    _aDepleted = true;
                }
            }
            catch
            {
                _aDepleted = true;
            }
        }

        private void MoveNextB(DateTime? dateTime = null)
        {
            try
            {
                if (!_b.MoveNext(dateTime))
                {
                    _bDepleted = true;
                }
            }
            catch
            {
                _bDepleted = true;
            }
        }

        private void MoveNextCurrent()
        {
            if (_currentConnection == null)
            {
                throw new InvalidOperationException(
                    "Initialize the enumerator first with 'movePrevious(DateTime)' or 'moveNext(DateTime)'");
            }

            if (_currentConnection.MoveNext())
            {
                return;
            }

            // The enumerator has depleted
            // We figure out which one and mark it as used
            if (_currentConnection == _a)
            {
                _aDepleted = true;
            }
            else
            {
                _bDepleted = true;
            }
        }


        public bool MoveNext(DateTime? dateTime = null)
        {
            if (dateTime != null)
            {
                _aDepleted = false;
                _bDepleted = false;
                // We have to initialize
                MoveNextA(dateTime);
                MoveNextB(dateTime);
            }
            else
            {
                MoveNextCurrent();
            }

            if (_aDepleted && _bDepleted)
            {
                // Both are depleted
                return false;
            }

            if (_bDepleted)
            {
                // _a is still loaded
                _currentConnection = _a;
                return true;
            }

            if (_aDepleted)
            {
                // _b is still loaded
                _currentConnection = _b;
                return true;
            }

            // Both are still loaded
            // We select the one that has the earliest departure time
            _currentConnection = _a.DepartureTime <= _b.DepartureTime ? _a : _b;
            return true;
        }


        private void MovePreviousA(DateTime? dateTime = null)
        {
            try
            {
                if (!_a.MovePrevious(dateTime))
                {
                    _aDepleted = true;
                }
            }
            catch
            {
                _aDepleted = true;
            }
        }

        private void MovePreviousB(DateTime? dateTime = null)
        {
            try
            {
                if (!_b.MovePrevious(dateTime))
                {
                    _bDepleted = true;
                }
            }
            catch
            {
                _bDepleted = true;
            }
        }

        private void MovePreviousCurrent()
        {
            if (_currentConnection == null)
            {
                throw new InvalidOperationException("Initialize the enumerator by");
            }

            if (_currentConnection.MovePrevious())
            {
                return;
            }

            // The enumerator has depleted
            // We figure out which one and null it
            if (_currentConnection == _a)
            {
                _aDepleted = true;
            }
            else
            {
                _bDepleted = true;
            }
        }


        public bool MovePrevious(DateTime? dateTime = null)
        {
            if (dateTime != null)
            {
                _aDepleted = false;
                _bDepleted = false;
                // We have to initialize
                MovePreviousA(dateTime);
                MovePreviousB(dateTime);
            }
            else
            {
                MovePreviousCurrent();
            }

            if (_aDepleted && _bDepleted)
            {
                // Both are depleted
                return false;
            }

            if (_bDepleted)
            {
                // _a is still loaded
                _currentConnection = _a;
                return true;
            }

            if (_aDepleted)
            {
                // _b is still loaded
                _currentConnection = _b;
                return true;
            }

            // Both are still loaded
            // We select the one that has the latest departure time
            _currentConnection = _a.DepartureTime >= _b.DepartureTime ? _a : _b;
            return true;
        }


        [Pure] public uint Id => _currentConnection.Id;
        [Pure] public string GlobalId => _currentConnection.GlobalId;
        [Pure] public ulong ArrivalTime => _currentConnection.ArrivalTime;

        [Pure] public ulong DepartureTime => _currentConnection.DepartureTime;
        [Pure] public ushort TravelTime => _currentConnection.TravelTime;
        [Pure] public ushort ArrivalDelay => _currentConnection.ArrivalDelay;
        [Pure] public ushort DepartureDelay => _currentConnection.DepartureDelay;
        [Pure] public ushort Mode => _currentConnection.Mode;

        [Pure] public TripId TripId => _currentConnection.TripId;
        [Pure] public LocationId DepartureStop => _currentConnection.DepartureStop;

        [Pure] public LocationId ArrivalStop => _currentConnection.ArrivalStop;
    }
}