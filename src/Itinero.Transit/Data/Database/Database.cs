using System.Collections.Generic;

namespace Itinero.Transit.Data
{
    // ReSharper disable once InconsistentNaming
    public interface InternalId
    {
        /// <summary>
        /// Gives the internal DatabaseId
        /// </summary>
        uint DatabaseId { get; }
    }
    public interface IDatabaseReader<in TId, in T>
        where TId : InternalId, new()
    {
        /// <summary>
        /// MoveNext will  
        /// </summary>
        /// <returns></returns>
        bool Get(TId id, T objectToWrite);

        /// <summary>
        /// Searches if this globalId is present in this database.
        /// If it is, it'll return true and assign the id of it into Tid.
        /// If not, the implementation is free to give back a clearly invalid value, such as all 'maxValue' for the field
        ///
        /// Note that Tids should be structs for performance
        /// </summary>
        /// <returns></returns>
        bool Get(string globalId, T objectToWrite);

        /// <summary>
        /// Identifies which database-IDS this database can handle
        /// </summary>
        IEnumerable<uint> DatabaseIds { get; }
    }

    public static class DatabaseExtensions
    {
        public static bool Get<TId, T>(
            this IDatabaseReader<TId, T> db, string globalId, out T found) where TId : InternalId, new() where T : new()
        {
            found = new T();
            return db.Get(globalId, found);
        }
        public static T Get<TId, T>(this IDatabaseReader<TId, T> db, TId id)
            where T : new() where TId : InternalId, new()
        {
            var t = new T();
            if (db.Get(id, t))
            {
                return t;
            }

            return default(T);
        }
    }
    public interface IDatabaseEnumerator<TId> where TId : struct
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