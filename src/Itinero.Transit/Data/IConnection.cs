using System.Collections.Generic;

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
        ConnectionId Id { get; }


        /// <summary>
        /// Gets the global id, often an Uri
        /// </summary>
        string GlobalId { get; }

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
        /// The first usage (least significant 2 bits) is to indicate Dropoff and pickup types:
        /// (Mode % 4) == 0 => Both pickup and dropoff are possible - the normal situation
        ///           == 1 => Only pickup is possible
        ///           == 2 => Only dropoff is possible
        ///           == 3 => Neither pickup nor dropoff are possible
        /// The second mode indicates if the train is cancelled
        /// (Mode & 4) == 4 indicates that the train is cancelled and can not be taken.
        /// It might still be desirable to include them in a search, e.g. to detect the route the traveller is used to and to display a clear warning to them.
        /// 
        /// </summary>
        ushort Mode { get; }

        /// <summary>
        /// Gets the trip id.
        /// </summary>
        TripId TripId { get; }

        /// <summary>
        /// Gets the departure stop id.
        /// </summary>
        LocationId DepartureStop { get; }

        /// <summary>
        /// Gets the arrival stop id.
        /// </summary>
        LocationId ArrivalStop { get; }
    }

    public static class ConnectionExtensions
    {
    }

    public class SimpleConnection
    {
        public const ushort ModeNormal = 0;
        public const ushort ModeGetOnOnly = 1;
        public const ushort ModeGetOffOnly = 2;
        public const ushort ModeCantGetOnOff = 3;

        public const ushort ModeCancelled = 4;

        public SimpleConnection()
        {
        }

        public SimpleConnection(
            ConnectionId id,
            string globalId,
            LocationId departureStop,
            LocationId arrivalStop,
            ulong departureTime,
            ushort travelTime,
            ushort arrivalDelay,
            ushort departureDelay,
            ushort mode,
            TripId tripId
        )
        {
            Id = id;
            DepartureTime = departureTime;
            TravelTime = travelTime;
            ArrivalDelay = arrivalDelay;
            DepartureDelay = departureDelay;
            Mode = mode;
            TripId = tripId;
            GlobalId = globalId;
            DepartureStop = departureStop;
            ArrivalStop = arrivalStop;
            ArrivalTime = departureTime + travelTime;
        }

        public ConnectionId Id { get; set; }
        public string GlobalId { get; set; }

        public ulong ArrivalTime { get; set; }

        public ulong DepartureTime { get; set; }

        public ushort TravelTime { get; set; }

        public ushort ArrivalDelay { get; set; }

        public ushort DepartureDelay { get; set; }

        public ushort Mode { get; set; }

        public TripId TripId { get; set; }

        public LocationId DepartureStop { get; set; }

        public LocationId ArrivalStop { get; set; }


        public bool CanGetOn()
        {
            var m = (Mode % 4);
            return m == 0 || m == 1;
        }

        public bool CanGetOff()
        {
            var m = (Mode % 4);
            return m == 0 || m == 2;
        }

        public bool IsCancelled()
        {
            return (Mode & ModeCancelled) == ModeCancelled;
        }


        public override bool Equals(object obj)
        {
            if (obj is SimpleConnection c)
            {
                return Equals(this, c);
            }

            return false;
        }


        public bool Equals(SimpleConnection x, SimpleConnection y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Id.Equals(y.Id) && string.Equals(x.GlobalId, y.GlobalId) && x.ArrivalTime == y.ArrivalTime &&
                   x.DepartureTime == y.DepartureTime && x.TravelTime == y.TravelTime &&
                   x.ArrivalDelay == y.ArrivalDelay && x.DepartureDelay == y.DepartureDelay && x.Mode == y.Mode &&
                   x.TripId.Equals(y.TripId) && x.DepartureStop.Equals(y.DepartureStop) &&
                   x.ArrivalStop.Equals(y.ArrivalStop);
        }

        public int GetHashCode(SimpleConnection obj)
        {
            unchecked
            {
                var hashCode = obj.Id.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.GlobalId.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.ArrivalTime.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.DepartureTime.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.TravelTime.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.ArrivalDelay.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.DepartureDelay.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.Mode.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.TripId.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.DepartureStop.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.ArrivalStop.GetHashCode();
                return hashCode;
            }
        }
    }
}