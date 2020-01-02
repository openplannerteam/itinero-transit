using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Transit.Data.Aggregators
{
    public class DatabaseAggregator<TId, T> : IDatabaseReader<TId, T>
        where TId : InternalId, new()
    {
        private readonly List<IDatabaseReader<TId, T>> _dbs;
        private readonly Dictionary<uint, IDatabaseReader<TId, T>> _idToDb;
        public IEnumerable<uint> DatabaseIds => _idToDb.Keys;


        public static IDatabaseReader<TId, T> CreateFrom(List<IDatabaseReader<TId, T>> dbs)
        {
            if (dbs.Count == 1)
            {
                return dbs[0];
            }
            return new DatabaseAggregator<TId, T>(dbs);
        }
        
        private DatabaseAggregator(List<IDatabaseReader<TId, T>> dbs)
        {
            _dbs = dbs;
            _idToDb = new Dictionary<uint, IDatabaseReader<TId, T>>();
            foreach (var db in _dbs)
            {
                foreach (var id in db.DatabaseIds)
                {
                    if (_idToDb.ContainsKey(id))
                    {
                        throw new ArgumentException("Multiple databases are responsible for database nr " + id);
                    }

                    _idToDb[id] = db;
                }
            }
        }

        public bool TryGet(TId id, out T t)
        {
            if (_idToDb.TryGetValue(id.DatabaseId, out var db))
            {
                return db.TryGet(id, out t);
            }

            t = default(T);
            return false;
        }

        public List<T> GetAll(List<TId> ids)
        {
            var values = new List<T>();

            foreach (var id in ids)
            {
                if (!TryGet(id, out var stop))
                {
                    throw new ArgumentException($"Stop with id {id} not found");
                }
                values.Add(stop);
            }
            return values;
        }

        public bool TryGetId(string globalId, out TId id)
        {
            foreach (var db in _dbs)
            {
                if (db.TryGetId(globalId, out id))
                {
                    return true;
                }
            }

            id = default(TId);
            return false;
        }


        public IEnumerator<T> GetEnumerator()
        {
            return new AggregateEnumerator<TId, T>(_dbs);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class AggregateEnumerator<TId, T> : IEnumerator<T>
        where TId : InternalId, new()
    {
        private readonly List<IEnumerator<T>> _dbs;
        private int _currentDb;

        public AggregateEnumerator(IEnumerable<IDatabaseReader<TId, T>> dbs)
        {
            _dbs = dbs.Select(db => db.GetEnumerator()).ToList();
        }

        public bool MoveNext()
        {
            if (_dbs[_currentDb].MoveNext())
            {
                return true;
            }

            _currentDb++;
            if (_currentDb >= _dbs.Count)
            {
                return false;
            }

            return MoveNext();
        }

        public void Reset()
        {
            _currentDb = 0;
        }

        object IEnumerator.Current => Current;

        public T Current => _dbs[_currentDb].Current;
        public void Dispose()
        {
            
        }
    }
}