using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Data
{
    public class Profile<T>
        where T : IJourneyStats<T>
    {
        public readonly T StatsFactory;
        public readonly ProfiledStatsComparator<T> ProfileComparator;

        public readonly IOtherModeGenerator InternalTransferGenerator;
        public readonly IOtherModeGenerator WalksGenerator;

        public Profile(IOtherModeGenerator internalTransferGenerator,
            IOtherModeGenerator walksGenerator,
            T statsFactory,
            ProfiledStatsComparator<T> profileComparator
        )
        {
            StatsFactory = statsFactory;
            ProfileComparator = profileComparator;
            InternalTransferGenerator = internalTransferGenerator;
            WalksGenerator = walksGenerator;
        }
    }
}