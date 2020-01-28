using System;

namespace Itinero.Transit.Data.Core
{
    [Serializable]
    public struct ConnectionId : InternalId
    {
        public static ConnectionId Invalid = new ConnectionId(uint.MaxValue, uint.MaxValue);

        public uint DatabaseId { get; }
        public ulong LocalId { get; }


        public ConnectionId(uint databaseId, ulong internalId)
        {
            DatabaseId = databaseId;
            LocalId = internalId;
        }

        public InternalId Create(uint databaseId, ulong localId)
        {
            return new ConnectionId(databaseId, localId);
        }

        public override string ToString()
        {
            return $"Connectionid({DatabaseId}, {LocalId})";
        }
    }
}