using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OsmSharp.Logging;
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

        public static void TestMoreStuff()
        {
            Log.Information("Starting");
            var test = new TestProfile(new DateTime(2018, 11, 26));
            var prof = test.CreateTestProfile();
            prof.IntermodalStopSearchRadius = 10000;

            var pcs = new ProfiledConnectionScan<TransferStats>(TestProfile.A, TestProfile.D,
                test.Moment(17, 00), test.Moment(19, 01), prof
            );


            var journeys = pcs.CalculateJourneys();
            
            
            var found = 0;
            var stats = "";
            foreach (var key in journeys.Keys)
            {
                var journeysFromPtStop = journeys[key];
                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var journey in journeysFromPtStop)
                {
                    Log.Information(journey.ToString(prof));
                    stats += $"{key}: {journey.Stats}\n";
                }

                // ReSharper disable once PossibleMultipleEnumeration
                found += journeysFromPtStop.Count();
            }

            Log.Information($"Got {found} profiles");
            Log.Information(stats);
        }


        static void Main(string[] args)
        {
            EnableLogging();
            Log.Information($"{args.Length} CLI params given");

            TestMoreStuff();
            /*
            // do staging, download & preprocess some data.
            var routerDb = BuildRouterDb.BuildOrLoad();

            // setup profile.
            var profile = Sncb.Profile("cache", BuildRouterDb.LocalBelgiumRouterDb);

            // specifiy the query data.
            var poperinge = new Uri("http://irail.be/stations/NMBS/008896735");
            var vielsalm = new Uri("http://irail.be/stations/NMBS/008845146");
            var startTime = new DateTime(2018, 11, 17, 10, 8, 00);
            var endTime = new DateTime(2018, 12, 17, 23, 0, 0);

            // Initialize the algorimth
            var eas = new EarliestConnectionScan<TransferStats>(
                poperinge, vielsalm, startTime, endTime,
                profile);
            var journey = eas.CalculateJourney();

            // Print the journey. Passing the profile means that human-unfriendly IDs can be replaced with names (e.g. 'Vielsalm' instead of 'https://irail.be/stations/12345')
            Log.Information(journey.ToString(profile));
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