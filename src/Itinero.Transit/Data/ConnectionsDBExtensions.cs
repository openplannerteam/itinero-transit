namespace Itinero.Transit.Data
{
    public static class ConnectionsDbExtensions
    {
        /// <summary>
        /// Gets a reader() which is loaded on the connection.
        /// Use this for testing only, it is slow
        /// </summary>
        /// <returns></returns>
        public static IConnection LoadConnection(this ConnectionsDb db, uint id)
        {
            var reader = db.GetReader();
            reader.MoveTo(id);
            return reader;
        }
        
    }
}