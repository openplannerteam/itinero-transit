using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero_Transit.CSA;
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
            var delijn = new DeLijnProvider(loader);
            var prov = new LocallyCachedConnectionsProvider(delijn, new LocalStorage("storage"));
            var closeToHome = prov.GetLocationsCloseTo(51.21576f, 3.22f, 250);

            var closeToTarget = prov.GetLocationsCloseTo(51.19738f, 3.21736f, 500);

            Log.Information($"Found {closeToHome.Count()} stops closeby, {closeToTarget.Count()} where we have to go");

            var testTime = new DateTime(2018, 10, 23, 10, 00, 00);
            var failOver = new DateTime(2018, 10, 23, 11, 00, 00);

            List<Journey<TransferStats>> startJourneys = new List<Journey<TransferStats>>();
            foreach (var uri in closeToHome)
            {
                Log.Information($"{uri} ({prov.GetCoordinateFor(uri).Name})");
                startJourneys.Add(new Journey<TransferStats>(uri, testTime, TransferStats.Factory));
            }

            foreach (var uri in closeToTarget)
            {
                Log.Information($"> {uri} ({prov.GetCoordinateFor(uri).Name})");
            }

            var eas = new EarliestConnectionScan<TransferStats>(
                startJourneys, new List<Uri>(closeToTarget), prov, failOver);
            
            var j = eas.CalculateJourney();
            Log.Information(j.ToString());
        }


        private static void Main(string[] args)
        {
            ConfigureLogging();

            Log.Information("Starting...");
            var startTime = DateTime.Now;
            var loader = new Downloader(caching: false);
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