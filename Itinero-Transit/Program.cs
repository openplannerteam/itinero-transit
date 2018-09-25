using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
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
        public static readonly string IRail = "http://graph.irail.be/sncb/connections?departureTime=";

        
        

        private static void Main(string[] args)
        {

            var bxlc = "Brussel-Centraal/Bruxelles-Central";
            var gent = "Gent-Sint-Pieters";
               
            ConfigureLogging();
            Log.Information("Starting...");


            var pcs = new ProfiledConnectionScan<TransferStats>
            (Stations.GetId("Brugge"), Stations.GetId(bxlc),
                DateTime.Now.AddHours(-3),
                TransferStats.Factory, TransferStats.ParetoCompare);
           // 2018-09-20T14:55:00.000Z
            var uriIRail = new Uri(IRail+$"{DateTime.Now.AddHours(3):yyyy-MM-ddTHH:mm:ss}.000Z");
            var pareto = pcs.CalculateJourneys(uriIRail);
            Log.Information("Found " + pareto.Count + " pareto points");
            var i = 0;
            foreach (var journey in pareto)
            {
                Log.Information($"{i}:\n {journey}");
                i++;
            }
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