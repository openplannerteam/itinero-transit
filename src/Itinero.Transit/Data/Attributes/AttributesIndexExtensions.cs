using System.Collections.Generic;

namespace Itinero.Transit.Data.Attributes
{
    /// <summary>
    /// Contains extensions for the attributes index.
    /// </summary>
    public static class AttributesIndexExtensions
    {
        /// <summary>
        /// Adds a new attributes collection.
        /// </summary>
        public static uint Add(this AttributesIndex index, IEnumerable<Attribute> attributes)
        {
            return index.Add(new AttributeCollection(attributes));
        }

        /// <summary>
        /// Adds a new tag collection.
        /// </summary>
        public static uint Add(this AttributesIndex index, params Attribute[] attributes)
        {
            return index.Add(new AttributeCollection(attributes));
        }
    }
}