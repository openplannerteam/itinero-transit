using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Journeys;
using Itinero.Transit.Logging;
using Itinero.Transit.Tests.Functional.Algorithms;
using Itinero.Transit.Tests.Functional.Algorithms.CSA;
using Itinero.Transit.Tests.Functional.Algorithms.Search;
using Itinero.Transit.Tests.Functional.Data;
using Itinero.Transit.Tests.Functional.IO;
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
        public static void Main(string[] args)
        {
            EnableLogging();

            var testAll = args.Length > 0 && args[0].Equals("--full-test-suite");
            testAll = true;
            
            if (testAll)
            {
                // These are all the tests, and will be run in full on the build server
                // Tests for devving are below
                LocalTests();
                InternetTests();
                SlowTests();
                // ReSharper disable once RedundantJumpStatement
                return;
            }
               

        }


        private static void LocalTests()
        {
            // new ConnectionEnumeratorAggregatorTest().Run((
            //     TransitDb.ReadFrom(TestAllAlgorithms.testDbs0429),
            //     new DateTime(2019, 04, 29)));

            var nmbs = TransitDb.ReadFrom(TestAllAlgorithms._nmbs, 0);
            var wvl = TransitDb.ReadFrom(TestAllAlgorithms._delijnWvl, 1);

            new StopEnumerationTest().Run(new List<TransitDb>()
            {
                nmbs, wvl
            });

            var db = new TestAllAlgorithms().ExecuteDefault();
            new TripHeadsignTest().Run(db);

            TestClosestStopsAndRouting(db);
            Log.Information("Running NoDuplicationTest");

            new NoDuplicationTest().Run();
            new ConnectionsDbDepartureEnumeratorTest().Run(db);
            new DelayTest().Run(true);


            var tdb = new TransitDb();
            tdb.UseOsmRoute("testdata/CentrumShuttle-Brugge.xml", DateTime.Today.ToUniversalTime(), DateTime.Today.AddDays(1).ToUniversalTime());
            new TestAllAlgorithms().ExecuteMultiModal();
        }

        public static void InternetTests()
        {
            foreach (var r in OsmTest.TestRelations)
            {
                var t = new OsmTest();
                t.Run(r);
            }

            new CachingTest().Run(true);
            var tdb = new TransitDb();
        }

        public static void SlowTests()
        {
            new MultipleLoadTest().Run(0);

            // This tests starts a timer which reloads a lot
            new TestAutoUpdating().Run(null);
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