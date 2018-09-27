using System;
using System.IO;
using Itinero_Transit.CSA;
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
        public static readonly string IRail = "http://graph.irail.be/sncb/connections?departureTime=";
        // ReSharper disable once InconsistentNaming
        public static readonly Uri IRailNow = new Uri("http://graph.irail.be/sncb/connections");


        private static void Main(string[] args)
        {
            var bxlc = "Brussel-Centraal/Bruxelles-Central";
            var gent = "Gent-Sint-Pieters";

            ConfigureLogging();
            Log.Information("Starting...");
            var startTime = DateTime.Now;

            try
            {
                var pcs = new ProfiledConnectionScan<TransferStats>
                (Stations.GetId("Poperinge"), Stations.GetId(bxlc),
                    DateTime.Now.AddHours(-3),
                    TransferStats.Factory, TransferStats.ProfileCompare);
                // 2018-09-20T14:55:00.000Z
                var uriIRail = new Uri(IRail + $"{DateTime.Now.AddHours(1).AddMinutes(30):yyyy-MM-ddTHH:mm:ss}.000Z");
                var pareto = pcs.CalculateJourneys(uriIRail);
                Log.Information("Found " + pareto.Count + " pareto points");
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