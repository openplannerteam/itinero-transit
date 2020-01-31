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
            //RunNMBS();
            RunDeLijn();
        }
        
        public static void RunNMBS()
        {
            var url = "http://planet.anyways.eu/transit/GTFS/belgium/nmbs/nmbs-latest.gtfs.zip";
            var fileName = "nmbs-latest.gtfs.zip";

            Download.Get(url, fileName);
            
            Run(fileName, DateTime.Now.Date.ToUniversalTime());
        }
        
        public static void RunDeLijn()
        {
            var url = "http://planet.anyways.eu/transit/GTFS/belgium/delijn/delijn-latest.gtfs.zip";
            var fileName = "delijn-latest.gtfs.zip";

            Download.Get(url, fileName);
            
            Run(fileName, DateTime.Now.Date.ToUniversalTime());
        }
        
        public static void Run(string path, DateTime day)
        {
            // read GTFS feed.
            Logging.Log.Verbose("Parsing GTFS...");
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