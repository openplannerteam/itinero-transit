using System;
using System.Collections.Generic;
using System.IO;
using Itinero_Transit.CSA;
using Itinero_Transit.CSA.ConnectionProviders;
using Itinero_Transit.CSA.Data;
using Itinero_Transit.LinkedData;
using JsonLD.Core;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Itinero_Transit
{
    public static class Program
    {

        private static void Main(string[] args)
        {

            Log.Information("Starting...");
            var startTime = DateTime.Now;

            try
            {
                var provider = new LinkedConnectionProvider(new Uri("http://graph.irail.be"));
                provider.GetTimeTable(new Uri("http://graph.irail.be/sncb/connections"));
             
            }
            catch (Exception e)
            {
                Log.Error(e, "Something went horribly wrong");
            }

            var endTime = DateTime.Now;
            Log.Information($"Calculating took {(endTime - startTime).TotalSeconds}");
            var downloader = SncbConnectionProvider.Loader;
            Log.Information(
                $"Downloading {downloader.DownloadCounter} entries took {downloader.TimeDownloading / 1000} sec; got {downloader.CacheHits} cache hits");
            
            
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