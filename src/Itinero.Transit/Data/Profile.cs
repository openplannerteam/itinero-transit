using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// The profile bundles all useful data and classes that are used by the algorithms
    /// </summary>
    public class Profile<T> where T : IJourneyStats<T>
    {
        public Profile(TransitDb.TransitDbSnapShot transitDbSnapShot,
            IOtherModeGenerator internalTransferGenerator, 
            IOtherModeGenerator walksGenerator,
            T statsFactory,ProfiledStatsComparator<T> profileComparator)
        {
            this.TransitDbSnapShot = transitDbSnapShot;
            InternalTransferGenerator = internalTransferGenerator;
            WalksGenerator = walksGenerator;
            StatsFactory = statsFactory;
            ProfileComparator = profileComparator;
        }
        
        public TransitDb.TransitDbSnapShot TransitDbSnapShot { get; }
        public IOtherModeGenerator InternalTransferGenerator { get; }
        public IOtherModeGenerator WalksGenerator { get; }
        public T StatsFactory { get; }
        public ProfiledStatsComparator<T> ProfileComparator { get; }

    }
}