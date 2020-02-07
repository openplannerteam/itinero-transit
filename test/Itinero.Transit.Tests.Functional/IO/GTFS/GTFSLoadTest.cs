using System;
using System.IO;
using System.Linq;
using GTFS;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Serialization;
using Itinero.Transit.IO.GTFS;
using Itinero.Transit.IO.VectorTiles;
using Itinero.Transit.Tests.Functional.Staging;
using Serilog;

namespace Itinero.Transit.Tests.Functional.IO.GTFS
{
    public static class GTFSLoadTest
    {
        public static void Run()
        {
            RunNMBS();
//            RunTec();
//            RunDeLijn();
//            RunMIVB();
        }
        
        public static void RunMIVB()
        {
            var url = "http://planet.anyways.eu/transit/GTFS/belgium/mivb/mivb-latest.gtfs.zip";
            var fileName = "mivb-latest.gtfs.zip";

            Download.Get(url, fileName);
            
            Run(fileName, DateTime.Now.Date.ToUniversalTime());
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
        
        public static void RunTec()
        {
            var url = "http://planet.anyways.eu/transit/GTFS/belgium/tec/tec-latest.gtfs.zip";
            var fileName = "tec-latest.gtfs.zip";

            Download.Get(url, fileName);
            
            Run(fileName, DateTime.Now.Date.ToUniversalTime());
        }
        
        public static void Run(string path, DateTime day)
        {
            // read GTFS feed.
            Logging.Log.Verbose($"Parsing GTFS: {path}...");
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

            (new[] {transitDb.Latest}).CalculateVectorTileTree(6, 14);

            using (var stream = File.Open("temp.transitdb", FileMode.Create))
            {
                transitDb.Latest.WriteTo(stream);
            }

            using (var stream = File.OpenRead("temp.transitdb"))
            {
                transitDb = new TransitDb(0);
                transitDb.GetWriter().ReadFrom(stream);
            }
        }
    }
}