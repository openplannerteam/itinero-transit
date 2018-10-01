using System;
using System.IO;
using System.Linq;
using CacheCow.Client;
using CacheCow.Common;
using Itinero_Transit.CSA;
using Itinero_Transit.CSA.ConnectionProviders;
using Itinero_Transit.CSA.Data;
using Itinero_Transit.LinkedData;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Itinero_Transit
{
    public static class Program
    {
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once InconsistentNaming


        private static void Main(string[] args)
        {

            
            var bxlc = "Brussel-Centraal/Bruxelles-Central";
            var gent = "Gent-Sint-Pieters";

            ConfigureLogging();
            Log.Information("Starting...");
            var startTime = DateTime.Now;

            try
            {
                var sncbprovider = new SncbConnectionProvider();
                var storage = new LocalStorage("timetables-cache");
                var provider = new LocallyCachedConnectionsProvider(sncbprovider, storage);
                Log.Information("Starting prefetch");
                provider.PreFetch(new DateTime(2018, 10, 2, 00, 01, 00),new DateTime(2018, 10, 2, 23, 59, 59));
                
                /*
                var pcs = new ProfiledConnectionScan<TransferStats>
                (Stations.GetId("Poperinge"), Stations.GetId("Vielsalm"), provider,
                    TransferStats.Factory, TransferStats.ProfileCompare);

                var profiles = pcs.CalculateJourneys(),
                    new DateTime(2018, 10, 1, 23, 59, 59));
                Log.Information($"Found {profiles.Count()} profiles");
                var pareto = new ParetoFrontier<TransferStats>(TransferStats.ParetoCompare).ParetoFront(profiles);
                Log.Information($"Found {pareto.Count()} pareto points");
                var i = 0;
                foreach (var journey in pareto)
                {
                    Log.Information($"{i}:\n {journey}");
                    i++;
                }*/
            }
            catch (Exception e)
            {
                Log.Error(e, "Something went horribly wrong");
            }

            var endTime = DateTime.Now;
            Log.Information($"Calculating took {(endTime - startTime).TotalSeconds}");
            Log.Information(
                $"Downloading {Downloader.DownloadCounter} entries took {Downloader.TimeDownloading / 1000} sec; got {Downloader.CacheHits} cache hits");
            
            
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