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

        public readonly Databases Databases;

        public Profile(Databases databases, T statsFactory, ProfiledStatsComparator<T> profileComparator)
        {
            Databases = databases;
            StatsFactory = statsFactory;
            ProfileComparator = profileComparator;
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
    public class Databases{
        public readonly ConnectionsDb ConnectionsDb;
        public readonly StopsDb StopsDb;
        public readonly IOtherModeGenerator InternalTransferGenerator;
        public readonly IOtherModeGenerator WalksGenerator;


        private readonly Action<DateTime, DateTime> _loadTimeWindow;
        private readonly DateTracker _loadedTimeWindows = new DateTracker();

        /// <summary>
        /// Create a profile with the given databases, statistics and a callback to load more data when needed
        /// </summary>
        /// <param name="connectionsDb"></param>
        /// <param name="stopsDb"></param>
        /// <param name="internalTransferGenerator"></param>
        /// <param name="walksGenerator"></param>
        /// <param name="loadTimeWindow">This function will be loaded whenever more data is needed</param>
        public Databases(ConnectionsDb connectionsDb, StopsDb stopsDb,
            IOtherModeGenerator internalTransferGenerator,
            IOtherModeGenerator walksGenerator,
            Action<DateTime, DateTime> loadTimeWindow = null)
        {
            ConnectionsDb = connectionsDb;
            StopsDb = stopsDb;
            InternalTransferGenerator = internalTransferGenerator;
            WalksGenerator = walksGenerator;
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