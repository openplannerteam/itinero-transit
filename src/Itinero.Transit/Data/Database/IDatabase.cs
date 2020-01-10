using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Itinero.Transit.Data
{
    // ReSharper disable once InconsistentNaming
    public interface InternalId
    {
        /// <summary>
        /// Gives the internal DatabaseId
        /// </summary>
        uint DatabaseId { get; }

        /// <summary>
        /// Gives the index in the internal database
        /// </summary>
        ulong LocalId { get; }

        /// <summary>
        /// Create a new InternalId
        /// </summary>
        /// <param name="databaseId"></param>
        /// <param name="localId"></param>
        /// <returns></returns>
        InternalId Create(uint databaseId, uint localId);
    }

    public interface IGlobalId
    {
        /// <summary>
        /// Gives the global id
        /// </summary>
        string GlobalId { get; }
        
        IReadOnlyDictionary<string, string> Attributes { get; }
    }

    public static class IGlobalIdExtensions
    {
        public static bool TryGetAttribute(this IGlobalId element, string key, out string value, string defaultValue = "")
        {
            if (element.Attributes == null)
            {
                value = defaultValue;
                return false;
            }

            if (element.Attributes.TryGetValue(key, out value))
            {
                return true;
            }

            value = defaultValue;
            return false;

        }
    }

    public interface IDatabase<TId, T> :
        IDatabaseReader<TId, T>
        where TId : struct, InternalId
        where T : IGlobalId
    {
        TId AddOrUpdate(T value);
    }

    /// <summary>
    /// The DatabaseReader is an object which, given an internal or global id, fetches the corresponding piece of data.
    /// 
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="T"></typeparam>
    public interface IDatabaseReader<TId, T> : IEnumerable<T>
        where TId : InternalId, new()
    {
        /// <summary>
        /// Gets the object corresponding with this id.
        /// A new object might be created for this.
        /// </summary>
        /// <returns></returns>
        bool TryGet(TId id, out T t);


        /// <summary>
        /// Searches if this globalId is present in this database.
        /// </summary>
        /// <returns>true iff found</returns>
        bool TryGetId(string globalId, out TId id);

        /// <summary>
        /// Identifies which database-IDS this database can handle
        /// </summary>
        IEnumerable<uint> DatabaseIds { get; }
    }

    public static class DatabaseExtensions
    {
        /// <summary>
        /// Gets the element, throws an exception if the id is not found
        /// </summary>
        public static T Get<TId, T>(this IDatabaseReader<TId, T> db, TId id)
            where TId : struct, InternalId where T : IGlobalId
        {
            if (!db.TryGet(id, out var t))
            {
                throw new ArgumentException($"The id {id} could not be found");
            }

            return t;
        }


        /// <summary>
        /// Gets the element, throws an exception if the id is not found
        /// </summary>
        public static T Get<TId, T>(this IDatabaseReader<TId, T> db, TId id, string notFoundMessage)
            where TId : struct, InternalId where T : IGlobalId
        {
            if (!db.TryGet(id, out var t))
            {
                throw new ArgumentException(notFoundMessage);
            }

            return t;
        }

        public static bool TryGet<TId, T>(this IDatabaseReader<TId, T> db, string globalId, out T value)
            where TId : InternalId, new()
        {
            if (!db.TryGetId(globalId, out var id))
            {
                value = default(T);
                return false;
            }

            return db.TryGet(id, out value);
        }

        /// <summary>
        /// Gets the element, throws an exception if the id is not found
        /// </summary>
        public static T Get<TId, T>(this IDatabaseReader<TId, T> db, string globalId, string notFoundMessage = null)
            where TId : struct, InternalId where T : IGlobalId
        {
            if (!db.TryGetId(globalId, out var id))
            {
                if (notFoundMessage != null)
                {
                 throw new ArgumentException(notFoundMessage);   
                }

                var exampleId = "";
                if (db.Any())
                {
                    exampleId = "An example id is " + db.First().GlobalId;
                }
                
                throw new ArgumentException($"GlobalId {globalId} not found. {exampleId}");
            }

            return db.Get(id);
        }

        public static TId GetId<TId, T>(this IDatabaseReader<TId, T> db, T t, string notFoundMessage = null)
            where TId : InternalId, new()
            where T : IGlobalId
        {
            return db.GetId(t.GlobalId, notFoundMessage);
        }
        
        public static TId GetId<TId, T>(this IDatabaseReader<TId, T> db, string globalId, string notFoundMessage = null)
            where TId : InternalId, new()
        {
            if (!db.TryGetId(globalId, out var id))
            {
                throw new ArgumentException(notFoundMessage ?? $"GlobalId {globalId} not found");
            }

            return id;
        }

        public static List<T> GetAll<TId, T>(this IDatabaseReader<TId, T> db, List<TId> ids)
            where TId : struct, InternalId where T : IGlobalId
        {
            var values = new List<T>();

            foreach (var id in ids)
            {
                values.Add(db.Get(id));
            }

            return values;
        }
    }


    public interface IClone<out T>
    {
        T Clone();
    }
    
}