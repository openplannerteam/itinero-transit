using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Data;
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
            // do staging, download & preprocess some data.
            var routerDb = BuildRouterDb.BuildOrLoad();

            // setup profile.
            var profile = Belgium.Sncb(new LocalStorage("cache"));
            
            // create a stops db and connections db.
            var stopsDb = new StopsDb();
            var stopsDbReader = stopsDb.GetReader();
            var connectionsDb = new ConnectionsDb();
            
            // get current connections for the next day or so.
            var cc = 0;
            var sc = 0;
            var timeTable = profile.GetTimeTable(DateTime.Now);
            do
            {
                Log.Information($"Processing timetable {timeTable.Id()}: [{timeTable.StartTime()},{timeTable.EndTime()}]");
                foreach (var connection in timeTable.Connections())
                {
                    var stop1Uri = connection.DepartureLocation();
                    var stop1Location = profile.GetCoordinateFor(stop1Uri);
                    var stop1Id = stop1Uri.ToString();
                    (uint localTileId, uint localId) stop1InternalId;
                    if (!stopsDbReader.MoveTo(stop1Id))
                    {
                        stop1InternalId = stopsDb.Add(stop1Id, stop1Location.Lon, stop1Location.Lon);
                        Log.Information($"Stop {stop1Id} added: {stop1InternalId}");
                        sc++;
                    }
                    else
                    {
                        stop1InternalId = stopsDbReader.Id;
                    }
                
                    var stop2Uri = connection.ArrivalLocation();
                    var stop2Location = profile.GetCoordinateFor(stop2Uri);
                    var stop2Id = stop2Uri.ToString();
                    (uint localTileId, uint localId) stop2InternalId;
                    if (!stopsDbReader.MoveTo(stop2Id))
                    {
                        stop2InternalId = stopsDb.Add(stop2Id, stop2Location.Lon, stop2Location.Lon);
                        Log.Information($"Stop {stop2Id} added: {stop2InternalId}");
                        sc++;
                    }
                    else
                    {
                        stop2InternalId = stopsDbReader.Id;
                    }

                    var connectionId = connection.Id().ToString();
                    connectionsDb.Add(stop1InternalId, stop2InternalId, connectionId,
                        connection.DepartureTime(),
                        (ushort) (connection.ArrivalTime() - connection.DepartureTime()).TotalSeconds, 0);
                    //Log.Information($"Connection {connectionId} added.");
                    cc++;
                }

                if ((timeTable.NextTableTime() - DateTime.Now) > new TimeSpan(1, 0, 0, 0))
                {
                    break;
                }
                
                var nextTimeTableUri = timeTable.NextTable();
                timeTable = profile.GetTimeTable(nextTimeTableUri);
            } while (true);
            
            Log.Information($"Added {sc} stops and {cc} connection.");

            var departureEnumerator = connectionsDb.GetDepartureEnumerator();
            var totalTime = 0;
            Log.Information($"Enumerating connections by departure time.");
            while (departureEnumerator.MoveNext())
            {
                //Log.Information($"Connection {departureEnumerator.GlobalId}: [{departureEnumerator.Stop1} -> {departureEnumerator.Stop2}]@{departureEnumerator.DepartureTime} ({departureEnumerator.TravelTime}s)");
                totalTime += departureEnumerator.TravelTime;
            }
            
            var arrivalEnumerator = connectionsDb.GetArrivalEnumerator();
            Log.Information($"Enumerating connections by arrival time.");
            while (arrivalEnumerator.MoveNext())
            {
                //Log.Information($"Connection {arrivalEnumerator.GlobalId}: [{arrivalEnumerator.Stop1} -> {arrivalEnumerator.Stop2}]@{arrivalEnumerator.DepartureTime} ({arrivalEnumerator.TravelTime}s)");
                totalTime += departureEnumerator.TravelTime;
            }
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