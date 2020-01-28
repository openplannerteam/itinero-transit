namespace Itinero.Transit.Data.Compacted
{
    public struct FullTripId : InternalId
    {
        public uint DatabaseId { get; }
        public ulong LocalId { get; }

        private FullTripId(uint databaseId, ulong localId)
        {
            DatabaseId = databaseId;
            LocalId = localId;
        }


        public InternalId Create(uint databaseId, ulong localId)
        {
            return new FullTripId(databaseId, localId);
        }
    }
}