using System;
using GTFS;
using Itinero.Transit.Data;
using Itinero.Transit.IO.GTFS;
using Serilog;

namespace Itinero.Transit.Tests.Functional.IO.GTFS
{
    public static class GTFSLoadTest
    {
        public static void Run(string path)
        {
            // read GTFS feed.
            Logging.Log.Verbose("Parsing GTFS...");
            var day = new DateTime(2020, 01, 28);
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
            
            var transitDb = new TransitDb(0);
            transitDb.LoadGTFS(feed, day, day);
        }
    }
}