using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Transit.Data;
using Itinero.Transit.Logging;
using Itinero.Transit.Tests.Functional.Algorithms;
using Itinero.Transit.Tests.Functional.Algorithms.Search;
using Itinero.Transit.Tests.Functional.Data;
using Itinero.Transit.Tests.Functional.FullStack;
using Itinero.Transit.Tests.Functional.IO;
using Itinero.Transit.Tests.Functional.IO.LC;
using Itinero.Transit.Tests.Functional.IO.LC.Synchronization;
using Itinero.Transit.Tests.Functional.IO.OSM;
using Itinero.Transit.Tests.Functional.Staging;
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

            Log.Information("Starting the functional tests...");
            var devTestsOnly = args.Length == 0 ||
                               !new List<string> {"--full-test-suite", "--full", "--test"}.Contains(args[0].ToLower());

            // use one router db globally.
            RouterDbStaging.Setup();


            // do some local caching.
            if (devTestsOnly)
            {
                return;
            }

            // These are all the tests, and will be run in full on the build server
            // Tests for devving are below this block
            LocalTests();
            try
            {
                InternetTests();
                SlowTests();
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Some website is down... Skipping internet tests");
            }
            catch (ArgumentException e)
            {
                if (!(e.InnerException is OperationCanceledException))
                {
                    throw;
                }

                Log.Warning("Some website is down... Skipping internet tests");
            }
        }


        private static void LocalTests()
        {
            var nmbs = TransitDb.ReadFrom(Constants.Nmbs, 0);

            // test read/write transit db.
            using (var memoryStream = WriteTransitDbTest.Default.Run(nmbs))
            {
                memoryStream.Seek(0, SeekOrigin.Begin);

                nmbs = ReadTransitDbTest.Default.Run(memoryStream);
            }

            new MixedDestinationTest().Run();
            new ConnectionsDbDepartureEnumeratorTest().Run(nmbs);
            var db = new TestAllAlgorithms().ExecuteDefault();
            new TestAllAlgorithms().ExecuteLongPeriod();
            new TestAllAlgorithms().ExecuteMultiModal();

            var wvl = TransitDb.ReadFrom(Constants.DelijnWvl, 1);

            new StopEnumerationTest().Run(new List<TransitDb>()
            {
                nmbs, wvl
            });

            new TripHeadsignTest().Run(db);

            TestClosestStopsAndRouting(db);
            Log.Information("Running NoDuplicationTest");

            new DelayTest().Run(true);
            new FullStackTest().TestAll();

            Log.Information("Running single TransitDb tests");
            new TestAllAlgorithms().ExecuteDefault();
            Log.Information("Running multi TransitDb tests");
        }

        public static void InternetTests()
        {
            foreach (var r in OsmTest.TestRelations)
            {
                var t = new OsmTest();
                t.Run(r);
            }

            new OsmRouteTest().Run();

            new InitialSynchronizationTest().Run();

            new NoDuplicationTest().Run();

            new CachingTest().Run(true);
            new Itinero2RoutingTest().Run();
            new FullStackTest().TestAll();
        }

        public static void SlowTests()
        {
            new MultipleLoadTest().Run();

            // This tests starts a timer which reloads a lot
            new TestAutoUpdating().Run();
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