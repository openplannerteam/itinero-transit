using System.Collections;
using System.Diagnostics.Contracts;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data.Simple
{
    public class SimpleConnectionsDb :
        SimpleDb<ConnectionId, Connection>, IConnectionsDb, IClone<SimpleConnectionsDb>
    {
        public SimpleConnectionsDb(uint dbId) : base(dbId)
        {
        }

        public SimpleConnectionsDb(SimpleDb<ConnectionId, Connection> copyFrom) : base(copyFrom)
        {
        }


        public void PostProcess()
        {
            Sort();
        }

        /// <summary>
        /// Sort the data by departure time of the connections.
        /// </summary>
        private void Sort()
        {
            Data.Sort(Connection.SortByDepartureTime);
        }

        /// <summary>
        /// Gives the departure time of the first connection in the DB, sorted by departure time
        /// </summary>
        public ulong EarliestDate => First()?.DepartureTime ?? ulong.MaxValue;

        /// <summary>
        /// Gives the departure time of the last connection in the DB, sorted by departure time
        /// </summary>
        public ulong LatestDate => Last()?.DepartureTime ?? ulong.MinValue;

        /// <summary>
        /// Returns the index of the first connection which departs after the given datetime
        /// </summary>
        /// <returns></returns>
        public int IndexOfFirst(ulong departureTime)
        {
            // We perform a binary search to get this first connection
            var l = 0;
            var r = Data.Count;
            while (l < r)
            {
                var m = (l + r) / 2;
                var mDep = Data[m].DepartureTime;
                if (mDep < departureTime)
                {
                    l = m + 1;
                }
                else
                {
                    r = m;
                }
            }

            return l;
        }

        internal ulong GetDepartureTimeOf(uint index)
        {
            return Data[(int) index].DepartureTime;
        }

        public IConnectionEnumerator GetEnumeratorAt(ulong departureTime)
        {
            return new SimpleConnectionEnumerator(this, Data.Count, IndexOfFirst(departureTime));
        }

        public SimpleConnectionsDb Clone()
        {
            return new SimpleConnectionsDb(this);
        }

        IConnectionsDb IClone<IConnectionsDb>.Clone()
        {
            return Clone();
        }


        internal class SimpleConnectionEnumerator : IConnectionEnumerator
        {
            private readonly int _count;
            private readonly uint _dbId;
            private int _next;
            private SimpleConnectionsDb _fallback;
            public ulong CurrentTime { get; private set; }

            public SimpleConnectionEnumerator(SimpleConnectionsDb fallback, int count, int startConnection)
            {
                _dbId = fallback.DatabaseId;
                _fallback = fallback;
                _count = count;
                _next = startConnection;
            }


            public bool MoveNext()
            {
                if (_count == 0 || _next >= _count)
                {
                    CurrentTime = ulong.MaxValue;
                    return false;
                }

                CurrentTime = _fallback.GetDepartureTimeOf((uint) _next);
                Current = new ConnectionId(_dbId, (uint) _next);
                _next++;
                return true;
            }

            public bool MovePrevious()
            {
                if (_next < 0 || _count == 0)
                {
                    CurrentTime = ulong.MinValue;
                    return false;
                }

                if (_next >= _count)
                {
                    _next = _count - 1;
                }

                CurrentTime = _fallback.GetDepartureTimeOf((uint) _next);
                Current = new ConnectionId(_dbId, (uint) _next);

                _next--;
                return true;
            }

            public void Reset()
            {
                _next = 0;
                CurrentTime = _fallback.GetDepartureTimeOf((uint) _next);
            }

            [Pure] public ConnectionId Current { get; private set; }
            [Pure] object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}