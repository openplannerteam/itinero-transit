namespace Itinero.Transit.Data
{
    public static class ConnectionsDBExtensions
    {


        /// <summary>
        /// Gets a reader() which is loaded on the connection.
        /// Use this for testing only, it is slow
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static IConnection LoadConnection(this ConnectionsDb Db, uint id)
        {
            var reader = Db.GetReader();
            reader.MoveTo(id);
            return reader;
        }
        
    }
}