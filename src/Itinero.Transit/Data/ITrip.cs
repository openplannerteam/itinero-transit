using System.Diagnostics.Contracts;

namespace Itinero.Transit.Data
{
    using Itinero.Transit.Data.Attributes;

    public struct TripId
    {
        public readonly uint DatabaseId, InternalId;
        
        public static readonly TripId Walk = new TripId(uint.MaxValue, uint.MaxValue);

        public TripId(uint databaseId, uint internalId)
        {
            DatabaseId = databaseId;
            InternalId = internalId;
        }

        [Pure]
        public bool Equals(TripId other)
        {
            return DatabaseId == other.DatabaseId && InternalId == other.InternalId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is TripId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) DatabaseId * 397) ^ (int) InternalId;
            }
        }

        public override string ToString()
        {
            return $"Trip {DatabaseId}_{InternalId}";
        }
    }

    /// <summary>
    /// Abstract definition of a trip.
    /// </summary>
    public interface ITrip
    {
        /// <summary>
        /// Gets the global id, probably an URI representing this trip.
        /// </summary>
        string GlobalId { get; }

        /// <summary>
        /// Gets the local id in this database.
        /// </summary>
        TripId Id { get; }

        /// <summary>
        /// Gets the attributes.
        /// </summary>
        IAttributeCollection Attributes { get; }
    }
}