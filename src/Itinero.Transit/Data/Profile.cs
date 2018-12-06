using Itinero.Transit.Data.Walks;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// The profile bundles all useful data and classes that are used by the algorithms
    /// </summary>
    public class Profile<T>
    {
        public readonly ConnectionsDb ConnectionsDb;
        public readonly StopsDb StopsDb;
        public readonly IWalksGenerator WalksGenerator;
        public readonly T StatsFactory;
        
        

        public Profile(ConnectionsDb connectionsDb, StopsDb stopsDb, IWalksGenerator walksGenerator, T statsFactory)
        {
            ConnectionsDb = connectionsDb;
            StopsDb = stopsDb;
            WalksGenerator = walksGenerator;
            StatsFactory = statsFactory;
        }
    }
}