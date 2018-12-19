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

// ReSharper disable UnusedMember.Local

// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests.Functional
{
    public static class Program
    {
        private const string Gent = "http://irail.be/stations/NMBS/008892007";
        private const string Brugge = "http://irail.be/stations/NMBS/008891009";
        private const string Poperinge = "http://irail.be/stations/NMBS/008896735";
        private const string Vielsalm = "http://irail.be/stations/NMBS/008845146";
        private const string BrusselZuid = "http://irail.be/stations/NMBS/008814001";
        private const string Kortrijk = "http://irail.be/stations/NMBS/008896008";


        private static int _nrOfRuns = 1;

        private static readonly List<DefaultFunctionalTest> _allTests =
            new List<DefaultFunctionalTest>
            {
                EarliestConnectionScanTest.Default,
                LatestConnectionScanTest.Default,
                ProfiledConnectionScanTest.Default,
                EasPcsComparison.Default,
                EasLasComparison.Default
            };


        private static readonly Dictionary<string, DefaultFunctionalTest> _allTestsNamed =
            new Dictionary<string, DefaultFunctionalTest>
            {
                {"eas", EarliestConnectionScanTest.Default},
                {"las", LatestConnectionScanTest.Default},
                {"pcs", ProfiledConnectionScanTest.Default},
                {"easpcs", EasPcsComparison.Default},
                {"easlas", EasLasComparison.Default}
            };


        public static void Main(string[] args)
        {
            var tests = _allTests;
            if (args.Length > 0)
            {
                _nrOfRuns = int.Parse(args[0]);
            }

            if (args.Length > 1)
            {
                tests = new List<DefaultFunctionalTest>();
                for (int i = 1; i < args.Length; i++)
                {
                    var testName = args[i];
                    if (!_allTestsNamed.ContainsKey(testName))
                    {    
                        var keys = "";
                        foreach (var k in _allTestsNamed.Keys)
                        {
                            keys += k + ", ";
                        }
                        throw new ArgumentException($"No test named {testName} found. Try one (or more) of the following as argument: {keys}");
                    }

                    tests.Add(_allTestsNamed[testName]);
                }
            }

            EnableLogging();
            Log.Information("Starting the Functional Tests...");
            Log.Information("If you get a deserialization-exception: clear the cache");

            var date = DateTime.Now.Date; // LOCAL TIMES! //
            // test loading a connections db
            var db = IO.LC.LoadConnectionsTest.Default.Run((date.Date, new TimeSpan(1, 0, 0, 0)));
            ConnectionsDbDepartureEnumeratorTest.Default.Run(db.connections);
            TestClosestStopsAndRouting(db);


            var inputs = new List<(ConnectionsDb, StopsDb, string, string, DateTime, DateTime)>
            {
                (db.connections, db.stops, Brugge,
                    Gent,
                    date.Date.AddHours(10),
                    date.Date.AddHours(12)),
                (db.connections, db.stops, Brugge,
                    Poperinge,
                    date.Date.AddHours(10),
                    date.Date.AddHours(13)),
                (db.connections, db.stops, Brugge,
                    Kortrijk,
                    date.Date.AddHours(6),
                    date.Date.AddHours(20)),
                (db.connections, db.stops, Poperinge,
                    Vielsalm,
                    date.Date.AddHours(10),
                    date.Date.AddHours(18))
            };


            var failed = 0;
            var results = new Dictionary<string, int>();


            foreach (var t in tests)
            {
                var name = t.GetType().Name;
                results[name] = 0;
                foreach (var i in inputs)
                {
                    try
                    {
                        if (!t.RunPerformance(i, _nrOfRuns))
                        {
                            Log.Information($"{name} failed");
                            failed++;
                        }
                        else
                        {
                            results[name]++;
                        }
                    }
                    catch (Exception e)
                    {
                        failed++;
                        Log.Error(e.Message);
                        Log.Error(e.StackTrace);
                    }
                }
            }

            foreach (var t in tests)
            {
                var name = t.GetType().Name;
                Log.Information($"{name}: {results[name]}/{inputs.Count}");
            }

            if (failed > 0)
            {
                throw new Exception("Some tests failed");
            }
        }

        private static void TestClosestStopsAndRouting((ConnectionsDb connections, StopsDb stops) db)
        {
            StopSearchTest.Default.RunPerformance((db.stops, 4.336209297180176,
                50.83567623496864, 1000), _nrOfRuns);
            StopSearchTest.Default.RunPerformance((db.stops, 4.436824321746825,
                50.41119778957908, 1000), _nrOfRuns);
            StopSearchTest.Default.RunPerformance((db.stops, 3.329758644104004,
                50.99052927907061, 1000), _nrOfRuns);
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