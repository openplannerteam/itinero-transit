using Itinero.Transit.Data;

namespace Itinero.Transit.Tests.Data
{
    
    /// <summary>
    /// Dummy implementation of ICOnnection
    /// </summary>
    public class Connection : IConnection
    {
        private readonly uint _id;
        private readonly ulong _departureTime;
        private readonly ulong _arrivalTime;
        private readonly ushort _travelTime;
        private readonly uint _tripId;
        private readonly (uint localTileId, uint localId) _arrivalStop;
        private readonly (uint localTileId, uint localId) _departureStop;

        public Connection(uint id, ulong departureTime, ulong arrivalTime, uint tripId, (uint localTileId, uint localId) arrivalStop, (uint localTileId, uint localId) departureStop)
        {
            _id = id;
            _departureTime = departureTime;
            _arrivalTime = arrivalTime;
            _travelTime = (ushort) (arrivalTime - departureTime);
            _tripId = tripId;
            _arrivalStop = arrivalStop;
            _departureStop = departureStop;
        }

        public uint Id => _id;

        public ulong ArrivalTime => _arrivalTime;

        public ulong DepartureTime => _departureTime;

        public ushort TravelTime => _travelTime;

        public uint TripId => _tripId;

        public (uint localTileId, uint localId) DepartureStop => _departureStop;

        public (uint localTileId, uint localId) ArrivalStop => _arrivalStop;
    }
}