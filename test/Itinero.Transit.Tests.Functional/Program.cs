using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Logging;
using Itinero.Transit.Tests.Functional.Algorithms;
using Itinero.Transit.Tests.Functional.Algorithms.CSA;
using Itinero.Transit.Tests.Functional.Algorithms.Search;
using Itinero.Transit.Tests.Functional.Data;
using Itinero.Transit.Tests.Functional.IO.LC;
using Itinero.Transit.Tests.Functional.IO.LC.Synchronization;
using Itinero.Transit.Tests.Functional.IO.OSM;
using Itinero.Transit.Tests.Functional.Regression;
using Itinero.Transit.Tests.Functional.Utils;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Log = Serilog.Log;

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


            // Setup...
            RouterDbStaging.Setup();
            var nmbs = TransitDbCache.Get(StringConstants.Nmbs, 0);
            var wvl = TransitDbCache.Get(StringConstants.DelijnWvl, 1);
            var all = TransitDbCache.GetAll(StringConstants.TestDbs.ToList());

            // do some local caching.
            if (devTestsOnly)
            {
                var withTime = nmbs.SelectProfile(new DefaultProfile(0))
                    .SelectStops("http://irail.be/stations/NMBS/008811262", "http://irail.be/stations/NMBS/008811197")
                    .SelectTimeFrame(StringConstants.TestDate.AddHours(1), StringConstants.TestDate.AddHours(10));

                new ProfiledConnectionScanWithMetricAndIsochroneFilteringTest().Run(withTime);

                Logging.Log.Information("Ran the devtests. Exiting now. Use --full-test-suite to run everything");
                return;
            }


            // TODO make sure IRail can handle this one          new MultipleLoadTest().Run();

            new IntermodalTestWithOtherTransport(nmbs, TestConstants.DefaultProfile)
                .RunOverMultiple(TestConstants.WithWalkTestCases);
            new IntermodalTestWithOtherTransport(nmbs, TestConstants.WithFirstLastMile)
                .RunOverMultiple(TestConstants.WithWalkTestCases);

            // The default setup - no arrival time given. A window will be constructed, but in some cases no journeys will be found if walking is significantly faster
            new ProductionServerMimickTest(nmbs, StringConstants.TestDate, null)
                .RunOverMultiple(TestConstants.WithWalkAndPtTestCases);

            new ProductionServerMimickTest(nmbs, StringConstants.TestDate, StringConstants.TestDate.AddHours(12))
                .RunOverMultiple(TestConstants.WithWalkAndPtTestCases);

            new ProductionServerMimickTest(nmbs, StringConstants.TestDate, StringConstants.TestDate.AddHours(12))
                .RunOverMultiple(TestConstants.OpenHopperTestCases());


            new ConnectionsDbDepartureEnumeratorTest().Run((nmbs, 63155));
            new ReadWriteTest().Run((nmbs, 63155));

            MultiTestRunner.NmbsOnlyTester().RunAllTests();
            MultiTestRunner.DelijnNmbsTester().RunAllTests();


            new StopEnumerationTest().Run(new List<TransitDb> {nmbs, wvl});

            new TripHeadsignTest().RunOverMultiple(all);

            new StopSearchTest().RunOverMultiple(new List<(TransitDb db, double lon, double lat, double distance)>
            {
                (wvl, 4.336209297180176, 50.83567623496864, 1000),
                (wvl, 4.436824321746825, 50.41119778957908, 1000),
                (wvl, 3.329758644104004, 50.99052927907061, 1000)
            });


            new DelayTest().Run();

            new TestOsmLoadingIntoTransitDb().RunOverMultiple(TestConstants.OsmRelationsToTest);

            new UpdateTransitDbTest().Run();

            new InitialSynchronizationTest().Run();

            new NoDuplicationTest().Run();

            new CachingTest().Run();

            new TestAutoUpdating().Run();

            Logging.Log.Information("All tests done");
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