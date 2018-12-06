using System;

namespace Itinero.Transit.Data
{
    public interface IConnection
    {
        
        uint Id { get; }
        UInt64 ArrivalTime { get; }
        UInt64 DepartureTime { get; }
        ushort TravelTime { get; }
        uint TripId { get; }
        ulong DepartureLocation { get; }
        ulong ArrivalLocation { get; }

    }
}