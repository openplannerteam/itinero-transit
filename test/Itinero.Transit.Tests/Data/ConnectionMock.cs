using Itinero.Transit.Data;

namespace Itinero.Transit.Tests.Data
{
    internal class ConnectionMock : IConnection
    {
        public ConnectionMock(uint id, ulong departureTime, ulong arrivalTime, uint tripId,
            LocationId arrivalStop,LocationId departureStop)
        {
            Id = id;
            DepartureTime = departureTime;
            ArrivalTime = arrivalTime;
            TravelTime = (ushort) (arrivalTime - departureTime);
            TripId = tripId;
            ArrivalStop = arrivalStop;
            DepartureStop = departureStop;
            ArrivalDelay = 0;
            DepartureDelay = 0;
            Mode = 0;
        }

        public ConnectionMock(uint id, ulong departureTime, ulong arrivalTime, uint tripId,
            LocationId arrivalStop, LocationId departureStop, ushort mode)
        {
            Id = id;
            DepartureTime = departureTime;
            ArrivalTime = arrivalTime;
            TravelTime = (ushort) (arrivalTime - departureTime);
            TripId = tripId;
            ArrivalStop = arrivalStop;
            DepartureStop = departureStop;
            ArrivalDelay = 0;
            DepartureDelay = 0;
            Mode = mode;
        }

        public uint Id { get; }

        public ulong ArrivalTime { get; }

        public ulong DepartureTime { get; }

        public ushort ArrivalDelay { get; }

        public ushort DepartureDelay { get; }

        public ushort Mode { get; }

        public ushort TravelTime { get; }

        public uint TripId { get; }

        public LocationId DepartureStop { get; }

        public LocationId ArrivalStop { get; }
    }
}