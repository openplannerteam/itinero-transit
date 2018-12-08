using System;

namespace Itinero.Transit.Data
{
    public interface IConnection
    {
        
        uint Id { get; }
        ulong ArrivalTime { get; }
        ulong DepartureTime { get; }
        ushort TravelTime { get; }
        uint TripId { get; }
        (uint localTileId, uint localId) ArrivalStop { get; }
        (uint localTileId, uint localId) DepartureStop { get; }
    }
}