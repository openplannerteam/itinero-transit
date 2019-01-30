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

        public TransitDb.TransitDbSnapShot TransitDbSnapShot { get; }
        public readonly IOtherModeGenerator InternalTransferGenerator;
        public readonly IOtherModeGenerator WalksGenerator;

        public Profile(TransitDb.TransitDbSnapShot transitDbSnapShot, 
            IOtherModeGenerator internalTransferGenerator,
            IOtherModeGenerator walksGenerator,
            T statsFactory,
            ProfiledStatsComparator<T> profileComparator
            )
        {
            TransitDbSnapShot = transitDbSnapShot;
            StatsFactory = statsFactory;
            ProfileComparator = profileComparator;
            InternalTransferGenerator = internalTransferGenerator;
            WalksGenerator = walksGenerator;
        }
    }


    /// <summary>
    /// The profile bundles all useful data and classes that are used by the algorithms.
    /// It contains
    /// - The databases
    /// - The statistic generators and comparators
    /// - What time windows are loaded into the database
    /// - A callback to load more data, if necessary
    /// </summary>
    public class Databases
    {
        public readonly ConnectionsDb ConnectionsDb;
        public readonly StopsDb StopsDb;
        public readonly TripsDb TripsDb;

        private readonly Action<DateTime, DateTime> _loadTimeWindow;
        private readonly DateTracker _loadedTimeWindows = new DateTracker();

        /// <summary>
        /// Create a database set with the given databases and a callback to load more data when needed
        /// </summary>
        /// <param name="loadTimeWindow">This function will be loaded whenever more data is needed</param>
        public Databases(ConnectionsDb connectionsDb, StopsDb stopsDb, TripsDb tripsDb,
            Action<DateTime, DateTime> loadTimeWindow)
        {
            ConnectionsDb = connectionsDb;
            StopsDb = stopsDb;
            TripsDb = tripsDb;
            _loadTimeWindow = loadTimeWindow;
        }


        /// <summary>
        /// Imports the necessary connections.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="refresh">If 'true', then connections in the database will be overwritten</param>
        public void LoadTimeWindow(DateTime start, DateTime end, bool refresh = false)
        {
            if (refresh)
            {
                throw new NotImplementedException();
            }

            var gaps = _loadedTimeWindows.Gaps(start, end);
            foreach (var (gapStart, gapEnd) in gaps)
            {
                _loadedTimeWindows.AddTimeWindow(gapStart, gapEnd);
                _loadTimeWindow.Invoke(gapStart, gapEnd);
            }
        }
    }
}