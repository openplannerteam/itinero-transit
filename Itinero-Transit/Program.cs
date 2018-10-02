using System;
using System.IO;
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

        private static void Main(string[] args)
        {

            
            ConfigureLogging();
            Log.Information("Starting...");
            var startTime = DateTime.Now;

            try
            {
                var sncbprovider = new SncbConnectionProvider();
                var storage = new LocalStorage("timetables-for-testing-2018-10-02");
                var provider = new LocallyCachedConnectionsProvider(sncbprovider, storage);
                
                var pcs = new ProfiledConnectionScan<TransferStats>
                (Stations.Brugge, Stations.Gent, provider,
                    TransferStats.Factory, TransferStats.ProfileCompare, TransferStats.ParetoCompare);

                var pareto = pcs.CalculateJourneys(new DateTime(2018,10,2,10,00,00), 
                    new DateTime(2018, 10, 2, 18, 00, 00));
                Log.Information($"Found {pareto.Count} profiles");
                var i = 0;
                foreach (var journey in pareto)
                {
                    Log.Information($"{i}:\n {journey}");
                    i++;
                }
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