using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// The profile bundles all useful data and classes that are used by the algorithms
    /// </summary>
    public class Profile<T> where T : IJourneyStats<T>
    {
        public readonly ConnectionsDb ConnectionsDb;
        public readonly StopsDb StopsDb;
        public readonly IOtherModeGenerator WalksGenerator;
        public readonly T StatsFactory;
        public readonly ProfiledStatsComparator<T> ProfileComparator;


        public Profile(ConnectionsDb connectionsDb, StopsDb stopsDb,
            IOtherModeGenerator walksGenerator, T statsFactory,
            ProfiledStatsComparator<T> profileComparator)
        {
            ConnectionsDb = connectionsDb;
            StopsDb = stopsDb;
            WalksGenerator = walksGenerator;
            StatsFactory = statsFactory;
            ProfileComparator = profileComparator;
        }
    }
}