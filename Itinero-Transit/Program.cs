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
        public static readonly Uri IRail = LinkedObject.AsUri("http://graph.irail.be/sncb/connections");

        public static readonly Uri Brugge = LinkedObject.AsUri("http://irail.be/stations/NMBS/008891009");
        public static readonly Uri GentStP = LinkedObject.AsUri("http://irail.be/stations/NMBS/008892007");

        public static readonly Uri Poperinge = LinkedObject.AsUri("http://irail.be/stations/NMBS/008896735");
        public static readonly Uri Vielsalm = LinkedObject.AsUri("http://irail.be/stations/NMBS/008845146");

        private static void Main(string[] args)
        {
            ConfigureLogging();
            Log.Information("Starting...");
            var ecs = new EarliestConnectionScan(Poperinge, Vielsalm);
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