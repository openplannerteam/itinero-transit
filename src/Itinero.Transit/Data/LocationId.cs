using System.Diagnostics.Contracts;

namespace Itinero.Transit.Data
{
    public struct LocationId
    {
        public static LocationId Invalid = new LocationId(uint.MaxValue, uint.MaxValue, uint.MaxValue);


        public readonly uint DatabaseId, LocalTileId, LocalId;

        public LocationId(uint databaseId, uint localTileId, uint localId)
        {
            DatabaseId = databaseId;
            LocalTileId = localTileId;
            LocalId = localId;
        }


        [Pure]
        public bool Equals(LocationId other)
        {
            return DatabaseId == other.DatabaseId && LocalTileId == other.LocalTileId && LocalId == other.LocalId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is LocationId other && Equals(other);
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
    }
}