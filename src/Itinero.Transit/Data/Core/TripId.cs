using System;
using System.Diagnostics.Contracts;
using Itinero.Transit.OtherMode;

namespace Itinero.Transit.Data
{
    public struct TripId : InternalId
    {
        public uint DatabaseId { get;  }
        public readonly uint InternalId;

        public TripId(uint databaseId, uint internalId)
        {
            DatabaseId = databaseId;
            InternalId = internalId;
        }

        public TripId(IOtherModeGenerator otherModeGenerator):this(UInt32.MaxValue, 
            (uint) otherModeGenerator.OtherModeIdentifier().GetHashCode())
        {
            
        }

        public static readonly TripId Invalid = new TripId(uint.MaxValue, uint.MaxValue);

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

}