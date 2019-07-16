using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Transit.Data.Core
{
    public class DatabaseEnumeratorAggregator<TId, T> : IDatabaseReader<TId, T> where TId : InternalId, new()
    {
        /// <summary>
        /// Keeps track of the individually passed arguments
        /// </summary>
        private readonly IDatabaseReader<TId, T>[] _uniqueUnderlyingDatabases;

        /// <summary>
        /// An array, where, for any given `i`, `UnderlyingDatabases[i].DatabaseIds.Contains(i)` holds.
        /// In other words, if you want to get a database which can handle a certain `i`, `UnderlyingDatabases[i]` will be able to handle it
        /// </summary>
        private readonly IDatabaseReader<TId, T>[] _underlyingDatabases;

        public IEnumerable<uint> DatabaseIds { get; }

        public DatabaseEnumeratorAggregator(IReadOnlyList<IDatabaseReader<TId, T>> databases)
        {
            var maxCount = 0;
            var dbList = new List<uint>();
            DatabaseIds = dbList;
            _uniqueUnderlyingDatabases = databases.ToArray();

            foreach (var db in databases)
            {
                maxCount = (int) Math.Max(maxCount, db.DatabaseIds.Max());
            }

            _underlyingDatabases = new IDatabaseReader<TId, T>[maxCount+1];
            foreach (var db in databases)
            {
                foreach (var i in db.DatabaseIds)
                {
                    _underlyingDatabases[i] = db;
                    dbList.Add(i);
                }
            }
        }

        public bool Get(TId id, T objectToWrite)
        {
            return _underlyingDatabases[id.DatabaseId].Get(id, objectToWrite);
        }

        public bool Get(string globalId, T objectToWrite)
        {
            foreach (var db in _uniqueUnderlyingDatabases)
            {
                if (db.Get(globalId, objectToWrite))
                {
                    return true;
                }
            }

            return false;
        }
        
        public static IDatabaseReader<TId, T> CreateFrom(IDatabaseReader<TId, T> a, IDatabaseReader<TId, T> b)
        {

            return CreateFrom(
                new []{a, b}
                );
        }

        public static IDatabaseReader<TId, T> CreateFrom(IEnumerable<IDatabaseReader<TId, T>> sources)
        {
            var s = sources.ToList();
            if (s.Count == 0)
            {
                throw new ArgumentException("At least one reader is needed to merge");
            }

            if (s.Count == 1)
            {
                return s[0];
            }

            foreach (var elem in s)
            {
                if (elem is DatabaseEnumeratorAggregator<TId, T> aggr)
                {
                    s.Remove(elem);
                    s.AddRange(aggr._uniqueUnderlyingDatabases);
                }
            }

            return new DatabaseEnumeratorAggregator<TId, T>(s);
        }
    }
}