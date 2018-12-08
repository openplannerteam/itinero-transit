using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Xunit;

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
            Algorithms.CSA.EasTestBasic.Default.Run((db.connections, db.stops, BruggeUri, GentUri,
                DateTime.Now.Date.AddHours(10)));
            Algorithms.CSA.EasTestBasic.Default.Run((db.connections, db.stops, GentUri, BrusselZuid,
                DateTime.Now.Date.AddHours(10)));
            Algorithms.CSA.EasTestBasic.Default.Run((db.connections, db.stops, BruggeUri, Poperinge,
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