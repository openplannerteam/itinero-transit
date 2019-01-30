using System;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Data
{
    public class Profile<T>
        where T : IJourneyStats<T>
    {
        public readonly T StatsFactory;
        public readonly ProfiledStatsComparator<T> ProfileComparator;

        private readonly TransitDb TransitDb;
        public TransitDb.TransitDbSnapShot TransitDbSnapShot { get; }
        public readonly IOtherModeGenerator InternalTransferGenerator;
        public readonly IOtherModeGenerator WalksGenerator;

        public Profile(TransitDb.TransitDbSnapShot snapShot,
            IOtherModeGenerator internalTransferGenerator,
            IOtherModeGenerator walksGenerator,
            T statsFactory,
            ProfiledStatsComparator<T> profileComparator
        )
        {
            TransitDbSnapShot = snapShot;
            StatsFactory = statsFactory;
            ProfileComparator = profileComparator;
            InternalTransferGenerator = internalTransferGenerator;
            WalksGenerator = walksGenerator;
        }

        public Profile(TransitDb transitDb,
            IOtherModeGenerator internalTransferGenerator,
            IOtherModeGenerator walksGenerator,
            T statsFactory,
            ProfiledStatsComparator<T> profileComparator
        ) : this(transitDb.Latest,
            internalTransferGenerator, walksGenerator, statsFactory, profileComparator
        )
        {
            TransitDb = transitDb;
        }

        public Profile<T> LoadWindow(DateTime start, DateTime end)
        {
            if (TransitDb == null)
            {
                // Initialization is only for testing. We assume the data is already there
                // If not - let it crash and burn
                return this;
            }
            TransitDb.UpdateTimeFrame(start, end);
            return new Profile<T>(TransitDb, InternalTransferGenerator, WalksGenerator, StatsFactory,
                ProfileComparator);
        }
    }
}