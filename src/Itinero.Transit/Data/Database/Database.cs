using System.Collections.Generic;
using System.Timers;

namespace Itinero.Transit.Data
{
    public interface InternalId
    {
        /// <summary>
        /// Gives the internal DatabaseId
        /// </summary>
        uint DatabaseId { get; }
    }
    public interface IDatabaseReader<Tid, in T>
        where Tid : InternalId, new()
    {
        /// <summary>
        /// MoveNext will  
        /// </summary>
        /// <param name="objectToWrite"></param>
        /// <returns></returns>
        bool Get(Tid id, T objectToWrite);

        /// <summary>
        /// Searches if this globalId is present in this database.
        /// If it is, it'll return true and assign the id of it into Tid.
        /// If not, the implementation is free to give back a clearly invalid value, such as all 'maxValue' for the field
        ///
        /// Note that Tids should be structs for performance
        /// </summary>
        /// <param name="globalId"></param>
        /// <param name="foundId"></param>
        /// <returns></returns>
        bool Get(string globalId, T objectToWrite);

        /// <summary>
        /// Identifies which database-IDS this database can handle
        /// </summary>
        IEnumerable<uint> DatabaseIds { get; }
    }

    public static class DatabaseExtensions
    {
        public static bool Get<Tid, T>(
            this IDatabaseReader<Tid, T> db, string globalId, out T found) where Tid : InternalId, new() where T : new()
        {
            found = new T();
            return db.Get(globalId, found);
        }
        public static T Get<Tid, T>(this IDatabaseReader<Tid, T> db, Tid id)
            where T : new() where Tid : InternalId, new()
        {
            var t = new T();
            if (db.Get(id, t))
            {
                return t;
            }

            return default(T);
        }
    }
    public interface IDatabaseEnumerator<TId, in T> where TId : struct
    {
        /// <summary>
        /// Gives the first identifier.
        /// Returns null if the collection is empty
        /// </summary>
        /// <returns></returns>
        TId? First();
        /// <summary>
        /// Gives the next index based on the current
        /// SHould be Pure
        /// </summary>
        /// <param name="current"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        bool HasNext(TId current, out TId next);
    }


}