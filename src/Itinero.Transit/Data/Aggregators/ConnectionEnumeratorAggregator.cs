using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Transit.Data.Aggregators
{
    public class ConnectionEnumeratorAggregator : IConnectionEnumerator
    {
        private IConnectionEnumerator _currentConnection;

        private IConnectionEnumerator _a, _b;


        public ConnectionEnumeratorAggregator(List<IConnectionEnumerator> enumerators)
        {
            switch (enumerators.Count)
            {
                case 0: throw new ArgumentException("At least one enumerator is needed to fuse them");
                case 1:
                    _a = enumerators[0];
                    _b = null;
                    break;
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
                    _a = new ConnectionEnumeratorAggregator(enumerators.GetRange(0, halfway));
                    _b = new ConnectionEnumeratorAggregator(enumerators.GetRange(halfway, enumerators.Count-halfway));
                    break;
            }
        }

        public ConnectionEnumeratorAggregator(IConnectionEnumerator a, IConnectionEnumerator b)
        {
            _a = a;
            _b = b;
        }

        private void MoveNextA(DateTime? dateTime = null)
        {
            if (!_a.MoveNext(dateTime))
            {
                _a = null;
            }
        }

        private void MoveNextB(DateTime? dateTime = null)
        {
            if (!_b.MoveNext(dateTime))
            {
                _b = null;
            }
        }

        private void MoveNextCurrent()
        {
            if (_currentConnection == null)
            {
                throw new InvalidOperationException("Initialize the enumerator by");
            }

            if (_currentConnection.MoveNext())
            {
                return;
            }

            // The enumerator has depleted
            // We figure out which one and null it
            if (_currentConnection == _a)
            {
                _a = null;
            }
            else
            {
                _b = null;
            }
        }


        public bool MoveNext(DateTime? dateTime = null)
        {
            if (dateTime != null)
            {
                // We have to initialize
                MoveNextA(dateTime);
                MoveNextB(dateTime);
            }
            else
            {
                MoveNextCurrent();
            }

            if (_a == null && _b == null)
            {
                // Both are depleted
                return false;
            }

            if (_b == null)
            {
                // _a is still loaded
                _currentConnection = _a;
                return true;
            }

            if (_a == null)
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
            if (!_a.MovePrevious(dateTime))
            {
                _a = null;
            }
        }

        private void MovePreviousB(DateTime? dateTime = null)
        {
            if (!_b.MovePrevious(dateTime))
            {
                _b = null;
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
                _a = null;
            }
            else
            {
                _b = null;
            }
        }


        public bool MovePrevious(DateTime? dateTime = null)
        {
            if (dateTime != null)
            {
                // We have to initialize
                MovePreviousA(dateTime);
                MovePreviousB(dateTime);
            }
            else
            {
                MovePreviousCurrent();
            }

            if (_a == null && _b == null)
            {
                // Both are depleted
                return false;
            }

            if (_b == null)
            {
                // _a is still loaded
                _currentConnection = _a;
                return true;
            }

            if (_a == null)
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


        public uint Id => _currentConnection.Id;

        public ulong ArrivalTime => _currentConnection.ArrivalTime;

        public ulong DepartureTime => _currentConnection.DepartureTime;

        public ushort TravelTime => _currentConnection.TravelTime;

        public ushort ArrivalDelay => _currentConnection.ArrivalDelay;

        public ushort DepartureDelay => _currentConnection.DepartureDelay;

        public ushort Mode => _currentConnection.Mode;

        public uint TripId => _currentConnection.TripId;

        public (uint localTileId, uint localId) DepartureStop => _currentConnection.DepartureStop;

        public (uint localTileId, uint localId) ArrivalStop => _currentConnection.ArrivalStop;
    }
}