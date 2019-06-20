using System;
using System.Diagnostics.Contracts;
using Itinero.Transit.OtherMode;

namespace Itinero.Transit.Data
{
    using Attributes;

    public struct TripId
    {
        public readonly uint DatabaseId, InternalId;

        public TripId(uint databaseId, uint internalId)
        {
            DatabaseId = databaseId;
            InternalId = internalId;
        }

        public TripId(IOtherModeGenerator otherModeGenerator):this(UInt32.MaxValue, 
            (uint) otherModeGenerator.OtherModeIdentifier().GetHashCode())
        {
            
        }

        [Pure]
        public bool Equals(TripId other)
        {
            return DatabaseId == other.DatabaseId && InternalId == other.InternalId;
        }

        [Pure]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is TripId other && Equals(other);
        }

        [Pure]
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) DatabaseId * 397) ^ (int) InternalId;
            }
        }

        [Pure]
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