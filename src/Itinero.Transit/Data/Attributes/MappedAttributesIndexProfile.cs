using Reminiscence.Arrays;

namespace Itinero.Transit.Data.Attributes
{    
    /// <summary>
    /// Describes a deserialization profile.
    /// </summary>
    public class MappedAttributesIndexProfile
    {
        /// <summary>
        /// Gets or sets the data profile.
        /// </summary>
        public ArrayProfile DataProfile { get; set; }        

        /// <summary>
        /// A profile that tells the graph to use no caching.
        /// </summary>
        public static MappedAttributesIndexProfile NoCache = new MappedAttributesIndexProfile()
        {
            DataProfile = ArrayProfile.NoCache
        };
    }
}