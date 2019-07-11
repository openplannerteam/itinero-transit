using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Transit.Data
{
    public class DatabaseEnumeratorAggregator<Tid, T> : IDatabaseReader<Tid, T> where Tid : InternalId, new()
    {
        /// <summary>
        /// Keeps track of the individually passed arguments
        /// </summary>
        private readonly IDatabaseReader<Tid, T>[] _uniqueUnderlyingDatabases;

        /// <summary>
        /// An array, where, for any given `i`, `UnderlyingDatabases[i].DatabaseIds.Contains(i)` holds.
        /// In other words, if you want to get a database which can handle a certain `i`, `UnderlyingDatabases[i]` will be able to handle it
        /// </summary>
        private readonly IDatabaseReader<Tid, T>[] UnderlyingDatabases;

        public IEnumerable<uint> DatabaseIds { get; }

        public DatabaseEnumeratorAggregator(IReadOnlyList<IDatabaseReader<Tid, T>> databases)
        {
            var maxCount = 0;
            var dbList = new List<uint>();
            DatabaseIds = dbList;
            _uniqueUnderlyingDatabases = databases.ToArray();

            foreach (var db in databases)
            {
                maxCount = (int) Math.Max(maxCount, db.DatabaseIds.Max());
            }

            UnderlyingDatabases = new IDatabaseReader<Tid, T>[maxCount];
            foreach (var db in databases)
            {
                foreach (var i in db.DatabaseIds)
                {
                    UnderlyingDatabases[i] = db;
                    dbList.Add(i);
                }
            }
        }

        public bool Get(Tid id, T objectToWrite)
        {
            return UnderlyingDatabases[id.DatabaseId].Get(id, objectToWrite);
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
    }
}