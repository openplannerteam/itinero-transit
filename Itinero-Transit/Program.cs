using System;
using System.IO;
using Itinero_Transit.CSA;
using Itinero_Transit.LinkedData;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Formatting.Json;

namespace Itinero_Transit
{
    public static class Program
    {
        // ReSharper disable once InconsistentNaming
        public static readonly Uri IRail = new Uri("https://graph.irail.be/sncb/connections");

        private static void Main(string[] args)
        {
            ConfigureLogging();
            Log.Information("Starting...");
            var timeTable = new TimeTable(Downloader.DownloadJson(IRail));
            Log.Information(timeTable.ToString());
            
            Log.Information("Done");
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