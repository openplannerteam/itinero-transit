using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Logging;
using Itinero.Transit.Tests.Functional.Algorithms.CSA;
using Itinero.Transit.Tests.Functional.Algorithms.Search;
using Itinero.Transit.Tests.Functional.Data;
using Itinero.Transit.Tests.Functional.IO.LC;
using Itinero.Transit.Tests.Functional.IO.LC.Synchronization;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Log = Serilog.Log;

// ReSharper disable InconsistentNaming

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
        private const string Oostende = "http://irail.be/stations/NMBS/008891702";
        private const string Antwerpen = "http://irail.be/stations/NMBS/008821006"; // Antwerpen centraal
        private const string SintJorisWeert = "http://irail.be/stations/NMBS/008833159"; // Antwerpen centraal
        private const string Leuven = "http://irail.be/stations/NMBS/008833001"; // Antwerpen centraal

        private const string Howest = "https://data.delijn.be/stops/502132";
        private const string ZandStraat = "https://data.delijn.be/stops/500562";
        private const string AzSintJan = "https://data.delijn.be/stops/502083";


        private static readonly Dictionary<string, DefaultFunctionalTest> AllTestsNamed =
            new Dictionary<string, DefaultFunctionalTest>
            {
                {"eas", EarliestConnectionScanTest.Default},
                {"las", LatestConnectionScanTest.Default},
                {"pcs", ProfiledConnectionScanTest.Default},
                {"easpcs", EasPcsComparison.Default},
                {"easlas", EasLasComparison.Default},
                {"isochrone", IsochroneTest.Default}
            };


        private static void RunTests(IReadOnlyCollection<DefaultFunctionalTest> tests, int nrOfRuns)
        {
            EnableLogging();

            Log.Information("Starting the Functional Tests...");
            var fixedDate = true;

            DateTime date; // LOCAL TIMES! //
            if (fixedDate)
            {
                date = new DateTime(2019, 04, 09);
            }
            else
            {
                date = DateTime.Now;
            }


            new CachingTest().Run(true);


            TransitDb db;

            // db = LoadTransitDbTest.Default.Run((date.Date, new TimeSpan(1, 0, 0, 0)));
            // Log.Information("Running TestReadWrite");
//
            // db = new TestReadWrite().Run(db);
            var fileN = $"{date:yyyy-MM-dd}";
            try
            {
                string path;
                if (fixedDate)
                {
                    path = "fixed-test-cases-2019-04-09.transitdb";
                }
                else
                {
                    path = $"test-write-to-disk-{fileN}.transitdb";
                }

                db = TransitDb.ReadFrom(path, 0);
                Log.Information("Reused already existing tdb for testing");
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                if (fixedDate)
                {
                    throw e;
                }

                db = LoadTransitDbTest.Default.Run((date.Date, new TimeSpan(1, 0, 0, 0)));
                new TestWriteToDisk().Run((db, fileN));
            }


            Log.Information("Running TripHeadSignTest");
            TripHeadsignTest.Default.Run(db);

            Log.Information("Running NoDuplicationTest");
            new NoDuplicationTest().Run();
            // This tests starts a timer which reloads a lot
            //  new TestAutoUpdating().Run(null);

            // new MultipleLoadTest().Run(0);

            ConnectionsDbDepartureEnumeratorTest.Default.Run(db);

            TestClosestStopsAndRouting(db);


            // Tests all the algorithms on multiple inputs

            var inputs = new List<(TransitDb, string, string, DateTime, DateTime)>
            {
                (db, Brugge,
                    Gent,
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)), //*/
                (db, Poperinge, Brugge,
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)), //*/
                (db, Brugge, Poperinge,
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)),
                (db,
                    Oostende,
                    Brugge,
                    date.Date.AddHours(9),
                    date.Date.AddHours(11)),
                (db,
                    Brugge,
                    Oostende,
                    date.Date.AddHours(9),
                    date.Date.AddHours(11)),
                (db,
                    BrusselZuid,
                    Leuven,
                    date.Date.AddHours(9),
                    date.Date.AddHours(14)),
                (db,
                    Leuven,
                    SintJorisWeert,
                    date.Date.AddHours(9),
                    date.Date.AddHours(14)),
                (db,
                    BrusselZuid,
                    SintJorisWeert,
                    date.Date.AddHours(9),
                    date.Date.AddHours(14)),
                (db,
                    Brugge,
                    Kortrijk,
                    date.Date.AddHours(6),
                    date.Date.AddHours(20)),
                (db, Kortrijk,
                    Vielsalm,
                    date.Date.AddHours(9),
                    date.Date.AddHours(18)), //*/
                /* TODO Truly multimodal routes
                 (db, Howest,
                       Gent,
                       date.Date.AddHours(10),
                       date.Date.AddHours(18))
                //*/
            };

            var failed = 0;
            var results = new Dictionary<string, List<int>>();


            void RegisterFail<T>(string name, T input, int i)
            {
                Log.Error($"{name} failed on input #{i} {input}");
                failed++;
            }


            foreach (var t in tests)
            {
                var i = 0;
                var name = t.GetType().Name;
                results[name] = new List<int>();
                foreach (var input in inputs)
                {
                    try
                    {
                        if (!t.RunPerformance(input, nrOfRuns))
                        {
                            RegisterFail(name, input, i);
                        }
                        else
                        {
                            results[name].Add(i);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                        RegisterFail(name, input, i);
                    }

                    i++;
                }
            }

            foreach (var t in tests)
            {
                var name = t.GetType().Name;
                var fails = "";
                for (var j = 0; j < inputs.Count; j++)
                {
                    if (!results[name].Contains(j))
                    {
                        fails += $"{j}, ";
                    }
                }

                if (!string.IsNullOrEmpty(fails))
                {
                    fails = "Failed: " + fails;
                }

                Log.Information($"{name}: {results[name].Count}/{inputs.Count} {fails}");
            }

            if (failed > 0)
            {
                throw new Exception("Some tests failed");
            }
        }

        private static void TestClosestStopsAndRouting(TransitDb db)
        {
            StopSearchTest.Default.RunPerformance((db, 4.336209297180176,
                50.83567623496864, 1000));
            StopSearchTest.Default.RunPerformance((db, 4.436824321746825,
                50.41119778957908, 1000));
            StopSearchTest.Default.RunPerformance((db, 3.329758644104004,
                50.99052927907061, 1000));
        }

        public static void Main(string[] args)
        {
            List<DefaultFunctionalTest> tests = null;
            var nrOfRuns = 1;

// Handle input arguments
// Determine how many times each test should be run
            if (args.Length > 0)
            {
                nrOfRuns = int.Parse(args[0]);
            }

// Determine if tests should be skipped
            if (args.Length > 1)
            {
                tests = new List<DefaultFunctionalTest>();
                for (int i = 1; i < args.Length; i++)
                {
                    var testName = args[i];
                    if (!AllTestsNamed.ContainsKey(testName))
                    {
                        var keys = "";
                        foreach (var k in AllTestsNamed.Keys)
                        {
                            keys += k + ", ";
                        }

                        throw new ArgumentException(
                            $"No test named {testName} found. Try one (or more) of the following as argument: {keys}");
                    }

                    tests.Add(AllTestsNamed[testName]);
                }
            }

            if (tests == null)
            {
                tests = new List<DefaultFunctionalTest>();
                foreach (var t in AllTestsNamed)
                {
                    tests.Add(t.Value);
                }
            }

// And actually run the tests
            RunTests(tests, nrOfRuns);
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
            Logger.LogAction = (o, level, message, parameters) =>
            {
                if (loggingBlacklist.Contains(o))
                {
                    return;
                }

                if (!string.IsNullOrEmpty(o))
                {
                    message = $"[{o}] {message}";
                }

                if (level == TraceEventType.Verbose.ToString().ToLower())
                {
                    Log.Debug(message);
                }
                else if (level == TraceEventType.Information.ToString().ToLower())
                {
                    Log.Information(message);
                }
                else if (level == TraceEventType.Warning.ToString().ToLower())
                {
                    Log.Warning(message);
                }
                else if (level == TraceEventType.Critical.ToString().ToLower())
                {
                    Log.Fatal(message);
                }
                else if (level == TraceEventType.Error.ToString().ToLower())
                {
                    Log.Error(message);
                }
                else
                {
                    Log.Debug(message);
                }
            };
        }
    }
}