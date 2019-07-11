using Itinero.Transit.Data.Attributes;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// The class representing a single trip and related attributes.
    /// This can be rewritten and should not be shared amongst threads
    /// </summary>
    public class Trip
    {
        public string GlobalId { get; set; }
        public TripId Id { get; set; }
        public IAttributeCollection Attributes { get; set; }
    }
}