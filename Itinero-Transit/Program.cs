using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero_Transit.CSA;
using Itinero_Transit.CSA.ConnectionProviders;
using Itinero_Transit.CSA.Data;
using Itinero_Transit.LinkedData;
using JsonLD.Core;
using Newtonsoft.Json.Linq;
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
            
                var proc = new JsonLdProcessor(new Downloader(), new Uri("http://graph.irail.be/sncb/connections"));
                var jsonld = proc.LoadExpanded(new Uri("http://graph.irail.be/sncb/connections"));
                var prov = new LinkedConnectionProvider((JObject) jsonld["http://www.w3.org/ns/hydra/core#search"][0]);
                var tt = prov.GetTimeTable(DateTime.Now);
                Log.Information(tt.ToString());
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