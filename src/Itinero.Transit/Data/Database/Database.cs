using System.Collections.Generic;
using Itinero.Transit.Data.Core;

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
    
    /// <summary>
    /// The DatabaseReader is an object which, given an internal or global id, fetches the corresponding piece of data.
    /// 
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="T"></typeparam>
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

        public static T Get<TId, T>(this IDatabaseReader<TId, T> db, string uri) where TId : InternalId, new() where T : new()
        {
            var t = new T();
            if (db.Get(uri, t))
            {
                return t;
            }

            return default(T);
        }
    }
    
    
    /// <summary>
    /// THe DatabaseEnumerator is an object which enumerates the identifiers of all data pieces in the database
    /// </summary>
    /// <typeparam name="TId"></typeparam>
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
        /// Should be Pure
        /// </summary>
        /// <param name="current"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        bool HasNext(TId current, out TId next);
    }


}