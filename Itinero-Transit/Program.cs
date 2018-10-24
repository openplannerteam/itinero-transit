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


            var startDate = new DateTime(2018, 10, 24, 16, 30, 00);
            var endTime = new DateTime(2018, 10, 24, 17, 00, 00);
            var home = new Uri("https://www.openstreetmap.org/#map=19/51.21576/3.22048");
            var startLocation = OsmLocationMapping.Singleton.GetCoordinateFor(home);

            var station = new Uri("https://www.openstreetmap.org/#map=18/51.19738/3.21830");
            var endLocation = OsmLocationMapping.Singleton.GetCoordinateFor(station);

            var starts = deLijn.LocationProvider.GetLocationsCloseTo(startLocation.Lat, startLocation.Lon, 250);
            var ends = deLijn.WalkFromClosebyStops(endTime, endLocation, 250);

            var pcs = new ProfiledConnectionScan<TransferStats>(
                starts, ends, startDate, endTime, deLijn);


            var journeys = pcs.CalculateJourneys();
            var found = 0;
            foreach (var key in journeys.Keys)
            {
                var journeysFromPtStop = journeys[key];
                var target = deLijn.LocationProvider.GetCoordinateFor(new Uri(key));
                var walk = deLijn.FootpathTransferGenerator.GenerateFootPaths(startDate, startLocation, target);


                foreach (var journey in journeysFromPtStop)
                {
                    var diff = (walk.ArrivalTime() - journey.Connection.DepartureTime()).TotalSeconds;
                    var totalJourney = new Journey<TransferStats>(journey, walk.MoveTime(-diff));


                    Log.Information(totalJourney.ToString(deLijn.LocationProvider));
                }

                found += journeysFromPtStop.Count();
            }

            Log.Information($"Got {found} profiles");
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
                TestStuff(loader);
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