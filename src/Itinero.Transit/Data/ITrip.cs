using Itinero.Transit.Data.Attributes;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// Abstract definition of a trip.
    /// </summary>
    public interface ITrip
    {
        /// <summary>
        /// Gets the global id.
        /// </summary>
        string GlobalId { get; }
        
        /// <summary>
        /// Gets the id.
        /// </summary>
        uint Id { get; }
        
        /// <summary>
        /// Gets the attributes.
        /// </summary>
        IAttributeCollection Attributes { get; }
    }
}