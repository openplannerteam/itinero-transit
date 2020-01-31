using System;
using GTFS;
using Itinero.Transit.Data;
using Itinero.Transit.IO.GTFS;
using Itinero.Transit.Tests.Functional.Staging;
using Serilog;

namespace Itinero.Transit.Tests.Functional.IO.GTFS
{
    public static class GTFSLoadTest
    {
        public static void Run()
        {
            RunNMBS();
        }
        
        public static void RunNMBS()
        {
            var url = "http://planet.anyways.eu/transit/GTFS/belgium/nmbs/nmbs-latest.gtfs.zip";
            var fileName = "nmbs-latest.gtfs.zip";

            Download.Get(url, fileName);
            
            Run(fileName);
        }
        
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