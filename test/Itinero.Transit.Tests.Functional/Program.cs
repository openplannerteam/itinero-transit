using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Tests.Functional.Staging;
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

        static void Main(string[] args)
        {
            EnableLogging();
//            Log.Information($"{args.Length} CLI params given");
//            
//            // do staging, download & preprocess some data.
//            var routerDb = BuildRouterDb.BuildOrLoad();
//
//            // setup profile.
//            var profile = Belgium.Sncb(new LocalStorage("cache"));
//
//            // specify the query data.
//            var poperinge = new Uri("http://irail.be/stations/NMBS/008896735");
//            var vielsalm = new Uri("http://irail.be/stations/NMBS/008845146");
//            var startTime = new DateTime(2018, 11, 20, 11, 00, 00);
//            var endTime = new DateTime(2018, 11, 20, 23, 0, 0);
//
//            // Initialize the algorithm
//            var eas = new EarliestConnectionScan<TransferStats>(
//                poperinge, vielsalm, startTime, endTime,
//                profile);
//            var journey = eas.CalculateJourney();
//
//            // Print the journey. Passing the profile means that human-unfriendly IDs can be replaced with names (e.g. 'Vielsalm' instead of 'https://irail.be/stations/12345')
//            Log.Information(journey.ToString(profile));
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
        }
    }
}