using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Itinero.Transit.Data.Simple
{
    public abstract class SimpleDb<TId, T> : IDatabase<TId, T>
        where TId : struct, InternalId where T : IGlobalId
    {
        protected readonly uint DatabaseId;
        public IEnumerable<uint> DatabaseIds { get; }

        /// <summary>
        /// The actual data, neatly in a list
        /// </summary>
        protected readonly List<T> Data = new List<T>();

        /// <summary>
        /// A mapping of 'globalId' onto the index in _all
        /// </summary>
        private readonly Dictionary<string, uint> _globalIdMapping = new Dictionary<string, uint>();

        private TId _idFactory = new TId();

        protected SimpleDb(uint dbId)
        {
            DatabaseId = dbId;
            DatabaseIds = new[] {DatabaseId};
        }

        protected SimpleDb(SimpleDb<TId, T> copyFrom) : this(copyFrom.DatabaseId)
        {
            Data = new List<T>(copyFrom.Data);
            _globalIdMapping = new Dictionary<string, uint>(copyFrom._globalIdMapping);
        }

        public TId Add(T value)
        {
            var id = (uint) Data.Count;
            Data.Add(value);
            _globalIdMapping.Add(value.GlobalId, id);
            return (TId) _idFactory.Create(DatabaseId, id);
        }

        public TId AddOrUpdate(T value)
        {
            if (!_globalIdMapping.TryGetValue(value.GlobalId, out var index))
            {
                return Add(value);
            }

            Data[(int) index] = value;
            return (TId) _idFactory.Create(DatabaseId, index);
        }


        [Pure]
        public bool TryGet(TId id, out T stop)
        {
            if (id.DatabaseId != DatabaseId)
            {
                stop = default(T);
                return false;
            }

            if (id.LocalId >= (ulong) Data.Count)
            {
                stop = default(T);
                return false;
            }

            stop = Data[(int) id.LocalId];
            return true;
        }


        public T First()
        {
            if (Data.Count == 0)
            {
                return default(T);
            }

            return Data[0];
        }

        public T Last()
        {
            if (Data.Count == 0)
            {
                return default(T);
            }

            return Data[Data.Count - 1];
        }

        public bool TryGetId(string globalId, out TId id)
        {
            id = _idFactory;
            if (!_globalIdMapping.TryGetValue(globalId, out var index)) return false;
            id = (TId) _idFactory.Create(DatabaseId, index);

            return true;
        }


        [Pure]
        public IEnumerator<T> GetEnumerator()
        {
            return new SimpleDbEnumerator(this, Data.Count);
        }

        [Pure]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        private class SimpleDbEnumerator : IEnumerator<T>
        {
            private readonly SimpleDb<TId, T> _db;
            private readonly int _count;
            private int _next;

            public SimpleDbEnumerator(SimpleDb<TId, T> db, int count)
            {
                _db = db;
                _count = count;
                _next = 0;
            }

            public bool MoveNext()
            {
                if (_next >= _count)
                {
                    return false;
                }

                Current = _db.Data[_next];
                _next++;
                return true;
            }

            public void Reset()
            {
                _next = 0;
            }

            [Pure] public T Current { get; private set; }
            [Pure] object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}