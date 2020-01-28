namespace Itinero.Transit.Data.Compacted
{
    public struct RouteId : InternalId
    {
        private RouteId(uint databaseId, ulong localId)
        {
            DatabaseId = databaseId;
            LocalId = localId;
        }

        public uint DatabaseId { get; }
        public ulong LocalId { get; }

        public InternalId Create(uint databaseId, ulong localId)
        {
            return new RouteId(databaseId, localId);
        }
    }
}