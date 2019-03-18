using Itinero.Transit.Data;

namespace Itinero.Transit.Tests.Data
{
    internal class ConnectionMock : IConnection
    {
        private ushort _mode;

        public ConnectionMock(uint id, ulong departureTime, ulong arrivalTime, uint tripId, (uint localTileId, uint localId) arrivalStop, (uint localTileId, uint localId) departureStop)
        {
            Id = id;
            DepartureTime = departureTime;
            ArrivalTime = arrivalTime;
            TravelTime = (ushort) (arrivalTime - departureTime);
            TripId = tripId;
            ArrivalStop = arrivalStop;
            DepartureStop = departureStop;
        }

        public uint Id { get; }

        public ulong ArrivalTime { get; }

        public ulong DepartureTime { get; }

        public ushort ArrivalDelay { get; }

        public ushort DepartureDelay { get; }

        public ushort Mode => 0;

        public ushort TravelTime { get; }

        public uint TripId { get; }

        public (uint localTileId, uint localId) DepartureStop { get; }

        public (uint localTileId, uint localId) ArrivalStop { get; }
    }
}