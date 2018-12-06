using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Logging;
using Itinero.Transit.Tests.Functional.Staging;
using Itinero.Transit.Tests.Functional.Tests;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Itinero.Transit.Tests.Functional
{
    class Program
    {
        public static readonly List<FunctionalTest> AllTests = new List<FunctionalTest>
        {
            //new TransitDbLoadingTest(),
            new ConnectionsDbTest(),
            //new EasTestBasic(),
            //new EasTestAdvanced()
        };

        public static void Main(string[] args)
        {
            EnableLogging();
            Log.Information($"{args.Length} CLI params given");

            Log.Information("Starting the Functional Tests...");

            Log.Information("1) Running Staging");

            // do staging, download & preprocess some data.
            BuildRouterDb.BuildOrLoad();


            Log.Information("2) Starting tests");

            var tests = AllTests;

            var failed = new List<FunctionalTest>();

            for (int i = 0; i < tests.Count; i++)
            {
                Log.Information($"{i + 1}/{tests.Count}: Running {tests[i].GetType().Name}");

                var start = DateTime.Now;
//                try
//                {
                    tests[i].Test();

                    Log.Information($"{i + 1}/{tests.Count}: [OK]");
//                }
//                catch (Exception e)
//                {
//                    Log.Information($"{i + 1}/{tests.Count}: [FAILED] {e.Message}");
//                    Log.Error(e.Message + "\n" + e.StackTrace);
//                    failed.Add(tests[i]);
//
//
//                    if (tests.Count == 1)
//                    {
//                        throw;
//                    }
//                }

                var end = DateTime.Now;
                Log.Information($"{i + 1}/{tests.Count}: Took {(end - start).TotalMilliseconds}ms");
            }


            Log.Information("3) Tests are done");

            foreach (var failedTest in failed)
            {
                Log.Information($"Test {failedTest.GetType().Name} failed");
                
            }
            
            if (failed.Count > 0)
            {
                Log.Information($"{failed.Count} tests failed");
                throw new Exception($"{failed.Count} failed tests");
            }

            Log.Information($"All {tests.Count} tests successful!");
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