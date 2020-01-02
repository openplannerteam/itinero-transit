using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Itinero.Transit.OtherMode;

namespace Itinero.Transit.Data.Core
{
    [Serializable]
    public struct TripId : InternalId
    {
        public uint DatabaseId { get; }
        public ulong LocalId { get; }

        public TripId(uint databaseId, uint internalId)
        {
            DatabaseId = databaseId;
            LocalId = internalId;
        }

        public TripId(IOtherModeGenerator otherModeGenerator):this(UInt32.MaxValue, 
            (uint) otherModeGenerator.OtherModeIdentifier().GetHashCode())
        {
            
        }

        public InternalId Create(uint databaseId, uint localId)
        {
            return new TripId(databaseId, localId);
        }

        [Pure]
        public bool Equals(TripId other)
        {
            return DatabaseId == other.DatabaseId && LocalId == other.LocalId;
        }

        [Pure]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is TripId other && Equals(other);
        }

        [Pure]
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) DatabaseId * 397) ^ (int) LocalId;
            }
        }

        [Pure]
        public override string ToString()
        {
            return $"Trip {DatabaseId}_{LocalId}";
        }
    }

}