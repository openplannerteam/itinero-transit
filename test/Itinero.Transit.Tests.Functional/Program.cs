﻿using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Transit.Journeys;
using Itinero.Transit.Tests.Functional.Algorithms.CSA;
using Itinero.Transit.Tests.Functional.Algorithms.Search;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Itinero.Transit.Tests.Functional
{
    public class Program
    {
        private const string GentUri = "http://irail.be/stations/NMBS/008892007";
        private const string BruggeUri = "http://irail.be/stations/NMBS/008891009";
        private const string Poperinge = "http://irail.be/stations/NMBS/008896735";
        private const string Vielsalm = "http://irail.be/stations/NMBS/008845146";
        private const string BrusselZuid = "http://irail.be/stations/NMBS/008814001";
        
        public static void Main()
        {
            EnableLogging();
            Log.Information("Starting the Functional Tests...");

            // test loading a connections db
            var db = IO.LC.LoadConnectionsTest.Default.Run((DateTime.Now.Date, new TimeSpan(1, 0, 0, 0)));
            
            // test enumerating a connections db.
            Data.ConnectionsDbDepartureEnumeratorTest.Default.Run(db.connections);
            
            // run basic EAS test.
            var journey = EarliestConnectionScanTest.Default.RunPerformance((db.connections, db.stops, BruggeUri, GentUri,
                DateTime.Now.Date.AddHours(10)), 100);
            var json = journey.ToGeoJson(db.stops);
            journey = EarliestConnectionScanTest.Default.RunPerformance((db.connections, db.stops, GentUri, BrusselZuid,
                DateTime.Now.Date.AddHours(10)), 100);
            json = journey.ToGeoJson(db.stops);
            journey = EarliestConnectionScanTest.Default.RunPerformance((db.connections, db.stops, BruggeUri, Poperinge,
                DateTime.Now.Date.AddHours(10)), 100);
            json = journey.ToGeoJson(db.stops);
            journey = EarliestConnectionScanTest.Default.RunPerformance((db.connections, db.stops, BruggeUri, Vielsalm,
                DateTime.Now.Date.AddHours(10)), 100);
            json = journey.ToGeoJson(db.stops);
            
            // search closest stops.
            var stop1 = StopSearchTest.Default.RunPerformance((db.stops, 4.336209297180176,
                50.83567623496864, 1000), 100);
            var stop2 = StopSearchTest.Default.RunPerformance((db.stops, 4.436824321746825,
                50.41119778957908, 1000), 100);
            var stop3 = StopSearchTest.Default.RunPerformance((db.stops, 3.329758644104004,
                50.99052927907061, 1000), 100);
            
            // test routing from lat/lon to lat/lon
            Algorithms.CSA.EarliestConnectionScanTest.Default.Run((db.connections, db.stops, stop1.GlobalId, stop2.GlobalId,
                DateTime.Now.Date.AddHours(10)));
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