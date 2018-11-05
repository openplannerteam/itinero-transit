using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Belgium;
using Itinero.Transit.Tests.Functional.Staging;
using OsmSharp.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Itinero.Transit.Tests.Functional
{
    class Program
    {
        private static void Main(string[] args)
        {
            EnableLogging();

            Log.Information("Starting");
            var deLijn = DeLijn.Profile("storage", "belgium.routerdb");
            // The only difference with the test above:
            deLijn.IntermodalStopSearchRadius = 0;
            var startTime = new DateTime(2018,11,26,16, 00,00);
            var endTime = new DateTime(2018,11,26,17, 00,00);
           
            var home = new Uri("https://www.openstreetmap.org/#map=19/51.21576/3.22048");
            var startLocation = OsmLocationMapping.Singleton.GetCoordinateFor(home);
            var starts = deLijn.WalkToCloseByStops(startTime, startLocation, 1000);

            var station = new Uri("https://www.openstreetmap.org/#map=18/51.19738/3.21830");
            var endLocation = OsmLocationMapping.Singleton.GetCoordinateFor(station);
            var ends = deLijn.WalkFromCloseByStops(endTime, endLocation, 1000);


            var pcs = new ProfiledConnectionScan<TransferStats>(
                starts, ends, startTime, endTime, deLijn);


            var journeys = pcs.CalculateJourneys();
            var found = 0;
            var stats = "";
            TransferStats stat = null;
            foreach (var key in journeys.Keys)
            {
                var journeysFromPtStop = journeys[key];
                foreach (var journey in journeysFromPtStop)
                {
                    Log.Information(journey.ToString(deLijn.LocationProvider));
                    stats += $"{deLijn.GetNameOf(new Uri(key))}: {journey.Stats}\n";
                }

                found += journeysFromPtStop.Count();
            }

            
            
            
            Log.Information($"Got {found} profiles");
            Log.Information(stats);
        }

        private static void EnableLogging()
        {
            // initialize serilog.
            var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logFile = Path.Combine("logs", $"log-{date}.txt");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File(new JsonFormatter(), logFile)
                .WriteTo.Console()
                .CreateLogger();
            
            // link OsmSharp & Itinero logging to Serilog.
#if DEBUG
            var loggingBlacklist = new HashSet<string>();
#else
            var loggingBlacklist = new HashSet<string>();
#endif
            Logger.LogAction = (o, level, message, parameters) =>
            {
                if (loggingBlacklist.Contains(o))
                {
                    return;
                }
                if (level == TraceEventType.Verbose.ToString().ToLower())
                {
                    Log.Debug(string.Format("[{0}] {1} - {2}", o, level, message));
                }
                else if (level == TraceEventType.Information.ToString().ToLower())
                {
                    Log.Information(string.Format("[{0}] {1} - {2}", o, level, message));
                }
                else if (level == TraceEventType.Warning.ToString().ToLower())
                {
                    Log.Warning(string.Format("[{0}] {1} - {2}", o, level, message));
                }
                else if (level == TraceEventType.Critical.ToString().ToLower())
                {
                    Log.Fatal(string.Format("[{0}] {1} - {2}", o, level, message));
                }
                else if (level == TraceEventType.Error.ToString().ToLower())
                {
                    Log.Error(string.Format("[{0}] {1} - {2}", o, level, message));
                }
                else
                {
                    Log.Debug(string.Format("[{0}] {1} - {2}", o, level, message));
                }
            };
            Logging.Logger.LogAction = (o, level, message, parameters) =>
            {
                if (loggingBlacklist.Contains(o))
                {
                    return;
                }
                if (level == Logging.TraceEventType.Verbose.ToString().ToLower())
                {
                    Log.Debug(string.Format("[{0}] {1} - {2}", o, level, message));
                }
                else if (level == Logging.TraceEventType.Information.ToString().ToLower())
                {
                    Log.Information(string.Format("[{0}] {1} - {2}", o, level, message));
                }
                else if (level == Logging.TraceEventType.Warning.ToString().ToLower())
                {
                    Log.Warning(string.Format("[{0}] {1} - {2}", o, level, message));
                }
                else if (level == Logging.TraceEventType.Critical.ToString().ToLower())
                {
                    Log.Fatal(string.Format("[{0}] {1} - {2}", o, level, message));
                }
                else if (level == Logging.TraceEventType.Error.ToString().ToLower())
                {
                    Log.Error(string.Format("[{0}] {1} - {2}", o, level, message));
                }
                else
                {
                    Log.Debug(string.Format("[{0}] {1} - {2}", o, level, message));
                }
            };
        }
    }
}