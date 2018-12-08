namespace Itinero.Transit.Data
{
    /// <summary>
    /// Abstract definition of a connection.
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// Gets the internal connection id.
        /// </summary>
        uint Id { get; }
        
        /// <summary>
        /// Gets the arrival time.
        /// </summary>
        ulong ArrivalTime { get; }
        
        /// <summary>
        /// Gets the departure time.
        /// </summary>
        ulong DepartureTime { get; }
        
        /// <summary>
        /// Gets the travel time.
        /// </summary>
        ushort TravelTime { get; }
        
        /// <summary>
        /// Gets the trip id.
        /// </summary>
        uint TripId { get; }
        
        /// <summary>
        /// Gets the departure stop id.
        /// </summary>
        (uint localTileId, uint localId) DepartureStop { get; }
        
        /// <summary>
        /// Gets the arrival stop id.
        /// </summary>
        (uint localTileId, uint localId) ArrivalStop { get; }
    }
}