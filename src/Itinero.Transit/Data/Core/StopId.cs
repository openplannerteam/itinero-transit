using System.Diagnostics.Contracts;

namespace Itinero.Transit.Data.Core
{
    public struct StopId : InternalId
    {
        public static StopId Invalid = new StopId(uint.MaxValue, uint.MaxValue, uint.MaxValue);


        public uint DatabaseId { get; }
        public readonly uint LocalTileId;
        public readonly uint LocalId;

        public StopId(uint databaseId, uint localTileId, uint localId)
        {
            DatabaseId = databaseId;
            LocalTileId = localTileId;
            LocalId = localId;
        }


        [Pure]
        public bool Equals(StopId other)
        {
            return DatabaseId == other.DatabaseId && LocalTileId == other.LocalTileId && LocalId == other.LocalId;
        }

        [Pure]
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is StopId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) DatabaseId;
                hashCode = (hashCode * 397) ^ (int) LocalTileId;
                hashCode = (hashCode * 397) ^ (int) LocalId;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{(DatabaseId, LocalTileId, LocalId)}";
        }
    }
}