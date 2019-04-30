using Itinero.Transit.Data.Attributes;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// Abstract definition of a trip.
    /// </summary>
    public interface ITrip
    {
        /// <summary>
        /// Gets the global id, probably an URI representing this trip.
        /// </summary>
        string GlobalId { get; }
        
        /// <summary>
        /// Gets the local id in this database.
        /// </summary>
        (uint dbId, uint localId) Id { get; }
        
        /// <summary>
        /// Gets the attributes.
        /// </summary>
        IAttributeCollection Attributes { get; }
    }
}