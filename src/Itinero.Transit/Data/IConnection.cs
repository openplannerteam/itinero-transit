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
        public static bool CanGetOn(this IConnection c)
        {
            var m = (c.Mode % 4);
            return m == 0 || m == 1;
        }

        public static bool CanGetOff(this IConnection c)
        {
            var m = (c.Mode % 4);
            return m == 0 || m == 2;
        }

        public static bool IsCancelled(this IConnection c)
        {
            return (c.Mode & ModeCancelled) == ModeCancelled;
        }


        public const ushort ModeNormal = 0;
        public const ushort ModeGetOnOnly = 1;
        public const ushort ModeGetOffOnly = 2;
        public const ushort ModeCantGetOnOff = 3;

        public const ushort ModeCancelled = 4;
    }

    public class SimpleConnection : IConnection
    {
        public SimpleConnection(
            uint id, string globalId,
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

        public uint Id { get; }

        public string GlobalId { get; }

        public ulong ArrivalTime { get; }

        public ulong DepartureTime { get; }

        public ushort TravelTime { get; }

        public ushort ArrivalDelay { get; }

        public ushort DepartureDelay { get; }

        public ushort Mode { get; }

         public TripId TripId { get; }

        public LocationId DepartureStop { get; }

        public LocationId ArrivalStop { get; }
    }
}