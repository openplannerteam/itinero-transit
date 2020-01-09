using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Data.Core
{
    [Serializable]
    public class Connection : IGlobalId
    {
        public const ushort ModeGetOnOnly = 1;
        public const ushort ModeGetOffOnly = 2;

        private const ushort ModeCancelled = 4;

        public static ushort CreateMode(bool canGetOn, bool canGetOff, bool isCancelled)
        {
            var mode = (canGetOn ? ModeGetOnOnly : 0)
                       + (canGetOff ? ModeGetOffOnly : 0)
                       + (isCancelled ? ModeCancelled : 0);
            return (ushort) mode;
        }

        public static readonly Comparer<Connection> SortByDepartureTime = new ConnectionComparer();


        public string GlobalId { get; }

        public StopId DepartureStop { get; }

        public StopId ArrivalStop { get; }
        public ulong DepartureTime { get; }

        public ushort TravelTime { get; }

        public ulong ArrivalTime => DepartureTime + TravelTime;


        public ushort Mode { get; }

        public TripId TripId { get; }


        public readonly IReadOnlyDictionary<string, string> Attributes;


        public Connection(string globalId,
            StopId departureStop,
            StopId arrivalStop,
            ulong departureTime,
            ushort travelTime,
            ushort mode,
            TripId tripId,
            IReadOnlyDictionary<string, string> attributes = null
        )
        {
            DepartureTime = departureTime;
            TravelTime = travelTime;
            Mode = mode;
            TripId = tripId;
            GlobalId = globalId;
            DepartureStop = departureStop;
            ArrivalStop = arrivalStop;
            Attributes = attributes;

            if (DepartureTime == 0)
            {
                throw new ArgumentException("Trains are not supposed to leave at epoch, this will cause bugs.");
            }
        }


        public Connection(string globalId,
            StopId departureStop,
            StopId arrivalStop,
            ulong departureTime,
            ushort travelTime,
            TripId tripId
        ) : this(globalId, departureStop, arrivalStop, departureTime, travelTime, 0, tripId)
        {
        }

        public Connection(string globalId,
            StopId departureStop,
            StopId arrivalStop,
            DateTime departureTime,
            ushort travelTime,
            TripId tripId
        ) : this(globalId, departureStop, arrivalStop, departureTime.ToUnixTime(), travelTime, 0, tripId)
        {
        }


        public Connection(Connection c) : this(c.GlobalId, c.DepartureStop, c.ArrivalStop, c.DepartureTime,
            c.TravelTime, c.Mode, c.TripId)
        {
        }

        public Connection(StopId departureStop, StopId arrivalStop,
            string globalId, DateTime departureTime, ushort travelTime,
            TripId tripId, ushort mode)
        {
            DepartureStop = departureStop;
            ArrivalStop = arrivalStop;
            GlobalId = globalId;
            DepartureTime = departureTime.ToUnixTime();
            TravelTime = travelTime;
            TripId = tripId;
            Mode = mode;
        }

        public Connection(StopId departureStop, StopId arrivalStop, string globalId, ulong departureTime,
            ushort travelTime, TripId tripId)
        {
            DepartureStop = departureStop;
            ArrivalStop = arrivalStop;
            GlobalId = globalId;
            DepartureTime = departureTime;
            TravelTime = travelTime;
            TripId = tripId;
            Mode = 0;
        }


        [Pure]
        public string ToJson()
        {
            return
                $"{{id: {GlobalId}, departureTime:{DepartureTime.FromUnixTime():s}, arrivalTime:{ArrivalTime.FromUnixTime():s}, mode:{Mode}";
        }


        [Pure]
        public bool CanGetOn()
        {
            var m = Mode % 4;
            return m == 0 || m == 1;
        }

        [Pure]
        public bool CanGetOff()
        {
            var m = Mode % 4;
            return m == 0 || m == 2;
        }

        [Pure]
        public bool IsCancelled()
        {
            return (Mode & ModeCancelled) == ModeCancelled;
        }


        [Pure]
        public override bool Equals(object obj)
        {
            if (obj is Connection c)
            {
                return Equals(this, c);
            }

            return false;
        }


        [Pure]
        private static bool Equals(Connection x, Connection y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return string.Equals(x.GlobalId, y.GlobalId) &&
                   x.DepartureTime == y.DepartureTime && x.TravelTime == y.TravelTime &&
                   x.Mode == y.Mode &&
                   x.TripId.Equals(y.TripId) && x.DepartureStop.Equals(y.DepartureStop) &&
                   x.ArrivalStop.Equals(y.ArrivalStop);
        }

        [Pure]
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = GlobalId.GetHashCode();
                hashCode = (hashCode * 397) ^ ArrivalTime.GetHashCode();
                hashCode = (hashCode * 397) ^ DepartureTime.GetHashCode();
                hashCode = (hashCode * 397) ^ TravelTime.GetHashCode();
                hashCode = (hashCode * 397) ^ Mode.GetHashCode();
                hashCode = (hashCode * 397) ^ TripId.GetHashCode();
                hashCode = (hashCode * 397) ^ DepartureStop.GetHashCode();
                hashCode = (hashCode * 397) ^ ArrivalStop.GetHashCode();
                return hashCode;
            }
        }
    }

    internal class ConnectionComparer : Comparer<Connection>
    {
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public override int Compare(Connection x, Connection y)
        {
            return (int) ((long) x.DepartureTime - (long) y.DepartureTime);
        }
    }
}