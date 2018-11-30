using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Logging;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC;
using Itinero.Transit.Tests.Functional.Performance;
using Itinero.Transit.Tests.Functional.Staging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Itinero.Transit.Tests.Functional
{
    class Program
    {
        public static readonly DateTime TestDay = new DateTime(2018, 11, 26, 00, 00, 00);

        public static DateTime TestMoment(int hours, int minutes, int seconds = 0)
        {
            return TestDay.AddHours(hours).AddMinutes(minutes).AddSeconds(seconds);
        }

        static void Main(string[] args)
        {
            EnableLogging();
            Log.Information($"{args.Length} CLI params given");
            
            // do staging, download & preprocess some data.
            BuildRouterDb.BuildOrLoad();

            // setup profile.
            var profile = Belgium.Sncb(new LocalStorage("cache"));
            
            // create a stops db and connections db.
            var stopsDb = new StopsDb();
            var connectionsDb = new ConnectionsDb();
            
            // load connections for the next day.
            Action loadConnections = () =>
            {
                connectionsDb.LoadConnections(profile, stopsDb, (DateTime.Now, new TimeSpan(1, 0, 0, 0)));
            };
            loadConnections.TestPerf("Loading connections.");
            
            // enumerate connections by departure time.
            var tt = 0;
            var ce = 0;
            var departureEnumerator = connectionsDb.GetDepartureEnumerator();
            Action departureEnumeration = () =>
            {
                while (departureEnumerator.MoveNext())
                {
                    //var departureDate = DateTimeExtensions.FromUnixTime(departureEnumerator.DepartureTime);
                    //Log.Information($"Connection {departureEnumerator.GlobalId}: @{departureDate} ({departureEnumerator.TravelTime}s [{departureEnumerator.Stop1} -> {departureEnumerator.Stop2}])");
                    tt += departureEnumerator.TravelTime;
                    ce++;
                }
            };
            departureEnumeration.TestPerf("Enumerate by departure time.", 10);
            Log.Information($"Enumerated {ce} connections!");
//
//            // specify the query data.
//            var poperinge = new Uri("http://irail.be/stations/NMBS/008896735");
//            var vielsalm = new Uri("http://irail.be/stations/NMBS/008845146");
//            var startTime = new DateTime(2018, 11, 20, 11, 00, 00);
//            var endTime = new DateTime(2018, 11, 20, 23, 0, 0);
//
//            // Initialize the algorithm
//            var eas = new EarliestConnectionScan<TransferStats>(
//                poperinge, vielsalm, startTime, endTime,
//                profile);
//            var journey = eas.CalculateJourney();
//
//            // Print the journey. Passing the profile means that human-unfriendly IDs can be replaced with names (e.g. 'Vielsalm' instead of 'https://irail.be/stations/12345')
//            Log.Information(journey.ToString(profile));
            //*/
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
        }
    }
}