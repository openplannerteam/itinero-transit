namespace Itinero.Transit.Data.Attributes
{
    /// <summary>
    /// Abstract representation of an attribute collection.
    /// </summary>
    public interface IAttributeCollection : IReadonlyAttributeCollection
    {
        /// <summary>
        /// Gets the readonly flag.
        /// </summary>
        bool IsReadonly { get; }

        /// <summary>
        /// Adds or replaces an attribute.
        /// </summary>
        void AddOrReplace(string key, string value);

        /// <summary>
        /// Removes the attribute with the given key.
        /// </summary>
        bool RemoveKey(string key);

        /// <summary>
        /// Clears all attributes.
        /// </summary>
        void Clear();
    }
}