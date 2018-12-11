using System.Collections.Generic;

namespace Itinero.Transit.Data.Attributes
{
    /// <summary>
    /// Abstract representation of a readonly attribute collection.
    /// </summary>
    public interface IReadonlyAttributeCollection : IEnumerable<Attribute>
    {
        /// <summary>
        /// Gets the count.
        /// </summary>
        int Count { get; }
        
        /// <summary>
        /// Tries to get the value for the given key.
        /// </summary>
        bool TryGetValue(string key, out string value);
    }
}