namespace Itinero.Transit.Data.Attributes
{
    /// <summary>
    /// Represents an attributes.
    /// </summary>
    public struct Attribute
    {
        /// <summary>
        /// Creates a new attribute.
        /// </summary>
        public Attribute(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public string Value { get; set; }
        
        /// <summary>
        /// Gets a proper description of this attribute.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Key + "=" + this.Value;
        }
    }
}