using System;
using GTFS;
using Itinero.Transit.Data;
using Itinero.Transit.IO.GTFS.Data;
using Itinero.Transit.Logging;

namespace Itinero.Transit.IO.GTFS
{
    public static class TransitDbExtensions
    {
        /// <summary>
        /// Loads a GTFS into the given transit db.
        /// </summary>
        /// <param name="transitDb">The transit db.</param>
        /// <param name="path">The path to the archive containing the GTFS data.</param>
        /// <param name="startDate">The start date/time of the data to load.</param>
        /// <param name="endDate">The end date/time of the data to load</param>
        public static void LoadGTFS(this TransitDb transitDb, string path, DateTime startDate, DateTime endDate)
        {
            // read GTFS feed.
            IGTFSFeed feed = null;
            try
            {
                feed = new GTFSReader<GTFSFeed>().Read(path);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to read GTFS feed: {e}");
                throw;
            }
            
            // load feed.
            transitDb.LoadGTFS(feed, startDate, endDate);
        }
        
        /// <summary>
        /// Loads a GTFS into the given transit db.
        /// </summary>
        /// <param name="transitDb">The transit db.</param>
        /// <param name="feed">The path to the archive containing the GTFS data.</param>
        /// <param name="startDate">The start date/time of the data to load.</param>
        /// <param name="endDate">The end date/time of the data to load</param>
        public static void LoadGTFS(this TransitDb transitDb, IGTFSFeed feed, DateTime startDate, DateTime endDate)
        {
            var feedData = new FeedData(feed);
            var gtfs = new Gtfs2Tdb(feedData);
            var wr = transitDb.GetWriter();
            gtfs.AddDataBetween(wr, startDate, endDate);
            wr.Close();
        }
    }
}