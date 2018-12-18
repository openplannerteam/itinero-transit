using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Transit.Data;
using Itinero.Transit.Tests.Functional.Algorithms.CSA;
using Itinero.Transit.Tests.Functional.Algorithms.Search;
using Itinero.Transit.Tests.Functional.Data;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests.Functional
{
    public class Program
    {
        private const string GentUri = "http://irail.be/stations/NMBS/008892007";
        private const string BruggeUri = "http://irail.be/stations/NMBS/008891009";
        private const string Poperinge = "http://irail.be/stations/NMBS/008896735";
        private const string Vielsalm = "http://irail.be/stations/NMBS/008845146";
        private const string BrusselZuid = "http://irail.be/stations/NMBS/008814001";
        private const string Kortrijk = "http://irail.be/stations/NMBS/008896008";


        private static int _nrOfRuns = 1;

        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                _nrOfRuns = int.Parse(args[0]);
            }

            EnableLogging();
            Log.Information("Starting the Functional Tests...");
            Log.Information("If you get a deserialization-exception: clear the cache");

            var date = DateTime.Now.Date; // LOCAL TIMES! //
            // test loading a connections db
            var db = IO.LC.LoadConnectionsTest.Default.Run((date.Date, new TimeSpan(1, 0, 0, 0)));
            //         ConnectionsDbDepartureEnumeratorTest.Default.Run(db.connections);
            //         TestClosestStopsAndRouting(date, db);
            //         TestLas(date, db);
            //         TestPcs(date, db);
            //         TestEas(date, db);

            CompareEasPcs(date, db);
        }

        private static void CompareEasPcs(DateTime date, (ConnectionsDb connections, StopsDb stops) db)
        {
            EasPcsComparison.Default.Run(
                (db.connections, db.stops, Poperinge,
                    Vielsalm,
                    date.Date.AddHours(6),
                    date.Date.AddHours(20))
            );
        }

        private static void TestPcs(DateTime date, (ConnectionsDb connections, StopsDb stops) db)
        {
            ProfiledConnectionScanTest.Default.RunPerformance(
                (db.connections, db.stops, BruggeUri,
                    GentUri,
                    date.Date.AddHours(10),
                    date.Date.AddHours(12)), _nrOfRuns);
            ProfiledConnectionScanTest.Default.RunPerformance(
                (db.connections, db.stops, BruggeUri,
                    Poperinge,
                    date.Date.AddHours(10),
                    date.Date.AddHours(13)), _nrOfRuns);
            ProfiledConnectionScanTest.Default.RunPerformance(
                (db.connections, db.stops, BruggeUri,
                    Kortrijk,
                    date.Date.AddHours(6),
                    date.Date.AddHours(20)),
                _nrOfRuns);
            ProfiledConnectionScanTest.Default.RunPerformance(
                (db.connections, db.stops, Poperinge,
                    Vielsalm,
                    date.Date.AddHours(6),
                    date.Date.AddHours(20)),
                _nrOfRuns);
        }

        private static void TestClosestStopsAndRouting(DateTime date, (ConnectionsDb connections, StopsDb stops) db)
        {
            var stop1 = StopSearchTest.Default.RunPerformance((db.stops, 4.336209297180176,
                50.83567623496864, 1000), _nrOfRuns);
            var stop2 = StopSearchTest.Default.RunPerformance((db.stops, 4.436824321746825,
                50.41119778957908, 1000), _nrOfRuns);
            StopSearchTest.Default.RunPerformance((db.stops, 3.329758644104004,
                50.99052927907061, 1000), _nrOfRuns);
            EarliestConnectionScanTest.Default.RunPerformance((db.connections, db.stops, stop1.GlobalId,
                stop2.GlobalId,
                date.Date.AddHours(10)), _nrOfRuns);
        }

        private static void TestEas(DateTime date, (ConnectionsDb connections, StopsDb stops) db)
        {
            EarliestConnectionScanTest.Default.RunPerformance((db.connections, db.stops, BruggeUri,
                GentUri,
                date.Date.AddHours(10)), _nrOfRuns);
            EarliestConnectionScanTest.Default.RunPerformance((db.connections, db.stops, GentUri, BrusselZuid,
                date.Date.AddHours(10)), _nrOfRuns);

            EarliestConnectionScanTest.Default.RunPerformance((db.connections, db.stops, BruggeUri, Poperinge,
                date.Date.AddHours(10)), _nrOfRuns);

            EarliestConnectionScanTest.Default.RunPerformance((db.connections, db.stops, BruggeUri, Vielsalm,
                date.Date.AddHours(10)), _nrOfRuns);
            EarliestConnectionScanTest.Default.RunPerformance((db.connections, db.stops, Poperinge,
                Vielsalm,
                date.Date.AddHours(10)), _nrOfRuns);
        }


        private static void TestLas(DateTime date, (ConnectionsDb connections, StopsDb stops) db)
        {
            LatestConnectionScanTest.Default.RunPerformance((db.connections, db.stops, BruggeUri,
                GentUri,
                date.Date.AddHours(10)), _nrOfRuns);
            LatestConnectionScanTest.Default.RunPerformance((db.connections, db.stops, GentUri, BrusselZuid,
                date.Date.AddHours(10)), _nrOfRuns);
            LatestConnectionScanTest.Default.RunPerformance((db.connections, db.stops, BruggeUri, Poperinge,
                date.Date.AddHours(10)), _nrOfRuns);

            LatestConnectionScanTest.Default.RunPerformance((db.connections, db.stops, BruggeUri, Vielsalm,
                date.Date.AddHours(10)), _nrOfRuns);
            LatestConnectionScanTest.Default.RunPerformance((db.connections, db.stops, Poperinge, Vielsalm,
                date.Date.AddHours(10)), _nrOfRuns);
        }


        private static void EnableLogging()
        {
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