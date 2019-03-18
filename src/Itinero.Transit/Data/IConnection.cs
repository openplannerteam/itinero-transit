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
        /// Gets the arrival delay.
        /// </summary>
        ushort ArrivalDelay { get; }
        
        /// <summary>
        /// Gets the departure delay.
        /// </summary>
        ushort DepartureDelay { get; }
        
        
        /// <summary>
        /// An extra piece of state to sneak in more data.
        /// The first usage is to indicate Dropoff and pickup types:
        /// (Mode % 4) == 0 => Both pickup and dropoff are possible - the normal situation
        ///           == 1 => Only pickup is possible
        ///           == 2 => Only dropoff is possible
        ///           == 3 => Neither pickup nor dropoff are possible
        /// </summary>
        ushort Mode { get; }
        
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