using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace Itinero.Transit.Data.Core
{
    [Serializable]
    public struct StopId : InternalId
    {
        public static StopId Invalid = new StopId(uint.MaxValue, ulong.MaxValue);


        public uint DatabaseId { get; }
        public ulong LocalId { get; }

        public StopId(uint databaseId, ulong localId)
        {
            DatabaseId = databaseId;
            LocalId = localId;
        }

        [Pure]
        public InternalId Create(uint databaseId, uint localId)
        {
            return new StopId(databaseId, localId);
        }


        [Pure]
        public bool Equals(StopId other)
        {
            return DatabaseId == other.DatabaseId && LocalId == other.LocalId;
        }

        [Pure]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is StopId other && Equals(other);
        }

        [Pure]
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) DatabaseId;
                hashCode = (hashCode * 397) ^ (int) LocalId;
                return hashCode;
            }
        }

        [Pure]
        public override string ToString()
        {
            return $"{(DatabaseId, LocalId)}";
        }
    }
}