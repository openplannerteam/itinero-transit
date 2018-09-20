using System;
using System.IO;
using Itinero_Transit.CSA;
using Itinero_Transit.LinkedData;
using JsonLD.Util;
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

        public static readonly Uri Brugge = new Uri("https://irail.be/stations/NMBS/008891009");
        public static readonly Uri GentSP = new Uri("https://irail.be/stations/NMBS/008892007");

        private static void Main(string[] args)
        {
            ConfigureLogging();
            Log.Information("Starting...");
            var ecs = new EarliestConnectionScan(DateTime.Now,GentSP, Brugge);
            var j = ecs.CalculateJourney(IRail);
            Log.Information(j.ToString());
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