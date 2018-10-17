using System;
using System.Collections.Generic;
using System.IO;
using Itinero_Transit.CSA.ConnectionProviders;
using Itinero_Transit.CSA.ConnectionProviders.LinkedConnection.TreeTraverse;
using Itinero_Transit.CSA.Data;
using Itinero_Transit.CSA.LocationProviders;
using Itinero_Transit.LinkedData;
using JsonLD.Core;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Itinero_Transit
{
    public static class Program
    {
        private static void TestStuff(Downloader loader)
        {
            var uri = new Uri("http://dexagod.github.io/stoplocations/t0.jsonld");
            var wanted = new Uri("http://dexagod.github.io/stopsdata/d6.jsonld#12006");
            
            var proc=  new JsonLdProcessor(loader, new JsonLdOptions("http://dexagod.github.io/stopsdata/"));
            var traverser = new RdfTreeTraverser(uri, proc);
            Log.Information(traverser.GetCoordinateFor(wanted).ToString());

        }

        private static void Main(string[] args)
        {
            ConfigureLogging();

            Log.Information("Starting...");
            var startTime = DateTime.Now;
            var loader = new Downloader();
            try
            {
                TestStuff(loader);
            }
            catch (Exception e)
            {
                Log.Error(e, "Something went horribly wrong");
            }

            var endTime = DateTime.Now;
            Log.Information($"Calculating took {(endTime - startTime).TotalSeconds}");
            Log.Information(
                $"Downloading {loader.DownloadCounter} entries took {loader.TimeDownloading / 1000} sec; got {loader.CacheHits} cache hits");
        }

        private static Downloader DownloadEntireSNCBDay(DateTime start)
        {
            var sncbprov = new SncbConnectionProvider();
            var prov = new LocallyCachedConnectionsProvider(sncbprov, new LocalStorage("testdata"));
            prov.DownloadDay(start);
            return sncbprov.Downloader;
        }


        private static void ConfigureLogging()
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logFile = Path.Combine("logs", $"log-Itinero-Transit-{date}.txt");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File(new JsonFormatter(), logFile)
                .WriteTo.Console()
                .CreateLogger();
        }
    }
}