using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero_Transit.CSA;
using Itinero_Transit.CSA.ConnectionProviders;
using Itinero_Transit.CSA.Data;
using Itinero_Transit.CSA.LocationProviders;
using Itinero_Transit.LinkedData;
using JsonLD.Core;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Itinero_Transit
{
    public static class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void TestStuff(IDocumentLoader loader)
        {
            var storage = new LocalStorage("cache/delijn");
            var deLijn = DeLijn.Profile(loader, storage, "belgium.routerdb");

            var stops = new List<Uri>
            {
                new Uri("https://data.delijn.be/stops/502101"),
                new Uri("https://data.delijn.be/stops/507084"),
                new Uri("https://data.delijn.be/stops/507681"),
                new Uri("https://data.delijn.be/stops/507080"),
                new Uri("https://data.delijn.be/stops/500042")
            };


            var startTime = new DateTime(2018, 10, 26, 16, 00, 00);
            var endTime = new DateTime(2018, 10, 26, 17, 00, 00);
            var home = new Uri("https://www.openstreetmap.org/#map=19/51.21576/3.22048");
            var startLocation = OsmLocationMapping.Singleton.GetCoordinateFor(home);

            var station = new Uri("https://www.openstreetmap.org/#map=18/51.19738/3.21830");
            var endLocation = OsmLocationMapping.Singleton.GetCoordinateFor(station);

            var starts = deLijn.WalkToClosebyStops(startTime, startLocation, 1000);
            var ends = deLijn.WalkFromClosebyStops(endTime, endLocation, 1000);

            var pcs = new ProfiledConnectionScan<TransferStats>(
                starts, ends, startTime, endTime, deLijn);


            var journeys = pcs.CalculateJourneys();
            var found = 0;
            var stats = "";
            foreach (var key in journeys.Keys)
            {
                var journeysFromPtStop = journeys[key];
                foreach (var journey in journeysFromPtStop)
                {
                    Log.Information(journey.ToString(deLijn.LocationProvider));
                    stats += $"{key}: {journey.Stats}\n";
                }

                found += journeysFromPtStop.Count();
            }

            Log.Information($"Got {found} profiles");
            Log.Information(stats);
        }

        public static void TestStuff0(Downloader loader)
        {
            var deLijn = DeLijn.Profile(loader,
                new LocalStorage("cache/delijn"), "belgium.routerdb");
            var sncb = Sncb.Profile(loader, new LocalStorage("cache/sncb"), "belgium.routerdb");
            var merged = new ConnectionProviderMerger(new List<IConnectionsProvider>()
            {
                deLijn,
                sncb
            });

            var locs = new LocationCombiner(new List<ILocationProvider>
            {
                deLijn, sncb
            });

            // This moment (4AM) gives a neat mix of timetables:
            // Few trains, few buses so that the timetable of the buses are more then one minute long
            var moment = new DateTime(2018, 10, 30, 04, 00, 00);

            
            var tt = merged.GetTimeTable(moment);
            
            foreach (var conn in tt.Connections())
            {
                Log.Information($"{conn.DepartureTime():HH:mm} {conn.Id()}");
            }

            var sncbTT = sncb.GetTimeTable(moment);
            Log.Information($"NMBS Table: {sncbTT.StartTime():HH:mm} --> {sncbTT.EndTime():HH:mm}, {sncbTT.Connections().Count()} entries");
            var deLijnTT = deLijn.GetTimeTable(moment);
            Log.Information($"De Lijn Table: {deLijnTT.StartTime():HH:mm} --> {deLijnTT.EndTime():HH:mm}, {deLijnTT.Connections().Count()} entries");

            Log.Information(
                $"Synth table with {tt.Connections().Count()} entries,  starting at {tt.StartTime()} till {tt.EndTime()}");
        }


        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            ConfigureLogging();

            Log.Information("Starting...");
            var startTime = DateTime.Now;
            var loader = new Downloader();
            try
            {
                TestStuff0(loader);
            }
            catch (Exception e)
            {
                Log.Error(e, "Something went horribly wrong");
            }

            var endTime = DateTime.Now;
            Log.Information($"Calculating took {(endTime - startTime).TotalSeconds}");
            Log.Information(
                $"Downloading {loader.DownloadCounter} entries took {loader.TimeDownloading} sec; got {loader.CacheHits} cache hits");
        }


        private static void ConfigureLogging()
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logFile = Path.Combine("logs", $"log-Itinero-Transit-{date}.txt");
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