namespace Itinero.Transit.Data
{
    
    /// <summary>
    /// The profile bundles all useful data and classes that are used by the algorithms
    /// </summary>
    public class Profile
    {

        public readonly ConnectionsDb ConnectionsDb;
        public readonly StopsDb StopsDb;
        public readonly Func<Journey, uint, > WalksGenerator;
        
        
        public Profile(ConnectionsDb connectionsDb, StopsDb stopsDb)
        {
            ConnectionsDb = connectionsDb;
            StopsDb = stopsDb;
        }
    }
}