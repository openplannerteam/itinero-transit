using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data.Aggregators
{
    public class ConnectionsDbAggregator : IConnectionsDb
    {
        private readonly IDatabaseReader<ConnectionId, Connection> _data;
        private List<IConnectionsDb> _fallbacks;

        public ConnectionsDbAggregator(List<IConnectionsDb> fallbacks)
        {
            _fallbacks = fallbacks;
            _data = DatabaseAggregator<ConnectionId, Connection>.CreateFrom(
                fallbacks.Select(fb => (IDatabaseReader<ConnectionId, Connection>) fb).ToList());
        }

        public ulong EarliestDate
        {
            get
            {
                var min = ulong.MaxValue;
                foreach (var fallback in _fallbacks)
                {
                    min = Math.Min(min, fallback.EarliestDate);
                }

                return min;
            }
        }

        public ulong LatestDate
        {
            get
            {
                var max = ulong.MinValue;
                foreach (var fallback in _fallbacks)
                {
                    max = Math.Max(max, fallback.EarliestDate);
                }

                return max;
            }
        }


        IConnectionEnumerator IConnectionsDb.GetEnumeratorAt(ulong departureTime)
        {
            var enumerators = new List<IConnectionEnumerator>();
            foreach (var fallback in _fallbacks)
            {
                enumerators.Add(fallback.GetEnumeratorAt(departureTime));
            }

            return new ConnectionEnumeratorAggregator(enumerators);
        }

        public IEnumerator<Connection> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _data).GetEnumerator();
        }

        public bool TryGet(ConnectionId id, out Connection t)
        {
            return _data.TryGet(id, out t);
        }

        public bool TryGetId(string globalId, out ConnectionId id)
        {
            return _data.TryGetId(globalId, out id);
        }

        public IEnumerable<uint> DatabaseIds => _data.DatabaseIds;

        public void PostProcess()
        {
            foreach (var fallback in _fallbacks)
            {
                fallback.PostProcess();
            }
        }

        public IConnectionsDb Clone()
        {
            return new ConnectionsDbAggregator(_fallbacks);
        }

        public static IConnectionsDb CreateFrom(List<IConnectionsDb> connections)
        {
            if (connections.Count == 1)
            {
                return connections[0];
            }

            return new ConnectionsDbAggregator(connections);
        }
        
        public static IConnectionsDb CreateFrom(IEnumerable<TransitDbSnapShot> snapshots)
        {
            var dbs = snapshots.Select(sn => sn.ConnectionsDb).ToList();
            return CreateFrom(dbs);
        }
    }

    internal class ConnectionEnumeratorAggregator : IConnectionEnumerator
    {
        private readonly List<IConnectionEnumerator> _fallbacks;
        private int _currentFallback;

        public ulong CurrentTime { get; private set; }

        public ConnectionId Current { get; private set; }
        object IEnumerator.Current => Current;

        public ConnectionEnumeratorAggregator(List<IConnectionEnumerator> fallbacks)
        {
            _fallbacks = fallbacks;

            var lowestTime = ulong.MaxValue;
            for (var index = 0; index < _fallbacks.Count; index++)
            {
                var fallback = _fallbacks[index];
                // Every fallback is initialized on the first entry
                fallback.MoveNext();

                if (fallback.CurrentTime < lowestTime)
                {
                    _currentFallback = index;
                    lowestTime = fallback.CurrentTime;
                }
            }
        }


        private bool _initedForMovePrevious;

        public bool MovePrevious()
        {
            // All enumerators are initialized by MoveNext
            // This might have moved them out of range or onto one connection to far

            // We move them back and detect the highest one
            if (!_initedForMovePrevious)
            {
                _initedForMovePrevious = true;
                IConnectionEnumerator currentEnumerator = null;
                var highest = ulong.MinValue;
                for (var index = _fallbacks.Count - 1; index >=0; index--)
                {
                    var fallback = _fallbacks[index];
                    if (!fallback.MovePrevious())
                    {
                        continue;
                    }

                    if (fallback.CurrentTime == ulong.MinValue)
                    {
                        // Empty
                        continue;
                    }

                    if (highest < fallback.CurrentTime)
                    {
                        currentEnumerator = fallback;
                        highest = fallback.CurrentTime;
                        _currentFallback = index;
                    }
                }

                if (currentEnumerator == null)
                {
                    // All empty
                    return false;
                }

                Current = currentEnumerator.Current;
                CurrentTime = currentEnumerator.CurrentTime;
                return true;
            }
            else
            {
                // At this point, we have to move the currentEnumerator
                var currentEnumerator = _fallbacks[_currentFallback];

                var moved = currentEnumerator.MovePrevious();
                if (moved && currentEnumerator.CurrentTime == CurrentTime)
                {
                    // We can still use this enumerator
                    Current = currentEnumerator.Current;
                    // CurrentTime = currentEnumerator.CurrentTime;
                    return true;
                }


                // We have to search a different enumerator
                var highest = ulong.MinValue;
                currentEnumerator = null;
                for (var index = _fallbacks.Count - 1; index >=0; index--)
                {
                    var fallback = _fallbacks[index];

                    if (fallback.CurrentTime == ulong.MinValue)
                    {
                        // depleted
                        continue;
                    }

                    if (highest < fallback.CurrentTime)
                    {
                        currentEnumerator = fallback;
                        highest = fallback.CurrentTime;
                        _currentFallback = index;
                    }
                }

                if (currentEnumerator == null)
                {
                    // All empty
                    return false;
                }

                Current = currentEnumerator.Current;
                CurrentTime = currentEnumerator.CurrentTime;
                return true;
            }
        }


        public bool MoveNext()
        {
            var currentEnumerator = _fallbacks[_currentFallback];
            if (CurrentTime == 0)
            {
                // Not initialized yet -> we assign the current enumerator and thats it
                Current = currentEnumerator.Current;
                CurrentTime = currentEnumerator.CurrentTime;
                return true;
            }

            // The currentsEnumerator's value has been used
            var moved = currentEnumerator.MoveNext();

            if (moved && currentEnumerator.CurrentTime == CurrentTime)
            {
                // The current fallback can still be used: same time + did actually move
                Current = currentEnumerator.Current;
                CurrentTime = currentEnumerator.CurrentTime;

                return true;
            }

            // We have to find a different enumerator - we search the first one with the lowest currentTime
            var lowestDate = ulong.MaxValue;
            currentEnumerator = null;
            for (var i = 0; i < _fallbacks.Count; i++)
            {
                var enumerator = _fallbacks[i];
                if (enumerator.CurrentTime == ulong.MaxValue)
                {
                    // enumerator depleted
                    continue;
                }

                if (enumerator.CurrentTime < lowestDate)
                {
                    lowestDate = enumerator.CurrentTime;
                    currentEnumerator = enumerator;
                    _currentFallback = i;
                }
            }

            if (currentEnumerator == null)
            {
                // All enumerators are depleted
                // CurrentTime will automatically point to an enumerator with ulong.Maxvalue
                return false;
            }

            // We have found a new enumerator
            Current = currentEnumerator.Current;
            CurrentTime = currentEnumerator.CurrentTime;


            return true;
        }


        public void Reset()
        {
            _currentFallback = 0;
            foreach (var fallback in _fallbacks)
            {
                fallback.Reset();
            }
        }


        public void Dispose()
        {
            foreach (var fallback in _fallbacks)
            {
                fallback.Dispose();
            }
        }
    }
}