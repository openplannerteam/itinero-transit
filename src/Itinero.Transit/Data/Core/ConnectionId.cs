namespace Itinero.Transit.Data
{
    public struct ConnectionId : InternalId
    {
        public static ConnectionId Invalid = new ConnectionId(uint.MaxValue, uint.MaxValue);
        public uint DatabaseId { get; }
        public uint InternalId { get; }

        public ConnectionId(uint databaseId, uint internalId)
        {
            DatabaseId = databaseId;
            InternalId = internalId;
        }
    }
}