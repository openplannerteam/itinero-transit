using System;
using System.Collections.Generic;
using System.IO;
using Itinero_Transit.CSA;
using Itinero_Transit.CSA.ConnectionProviders;
using Itinero_Transit.CSA.ConnectionProviders.LinkedConnection;
using Itinero_Transit.CSA.Data;
using Itinero_Transit.LinkedData;
using JsonLD.Core;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

// ReSharper disable PossibleMultipleEnumeration

namespace Itinero_Transit
{
    public static class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void TestStuff(IDocumentLoader loader)
        {
            var storage = new LocalStorage("cache/delijn");
            var delijn = DeLijn.Profile(loader, storage, "belgium.routerdb");

            var ezelpoort =
                new Uri("https://data.delijn.be/stops/502101"); // or 502102 for the other location at ezelpoort
            var stationPerron2 = new Uri("https://data.delijn.be/stops/500042");
            var pcs = new ProfiledConnectionScan<TransferStats>(
                ezelpoort, stationPerron2, delijn);

            var startDate = new DateTime(2018, 10, 23, 16, 00, 00);
            var endTime = new DateTime(2018, 10, 23, 17, 00, 00);

            var journeys = pcs.CalculateJourneys(startDate, endTime);

            var front = pcs.GetProfileFor(ezelpoort);
            
            foreach (var journey in front.Frontier)
            {
                Log.Information(journey.ToString(delijn.LocationProvider));
            }


            var sncb = Sncb.Profile(loader, new LocalStorage("cache/sncb"), "belgium.routerdb");
            var brugge = new List<Location>(sncb.LocationProvider.GetLocationByName("Brugge"))[0];
            var brussel = new List<Location>(sncb.LocationProvider.GetLocationByName("Brussel-Centraal/Bruxelles-Central"))[0];
            
            pcs = new ProfiledConnectionScan<TransferStats>(brugge.Uri, brussel.Uri, sncb);
            var js = pcs.CalculateJourneys(DateTime.Now, DateTime.Now.AddHours(2));
            foreach (var j in js)
            {
                Log.Information(j.ToString(sncb.LocationProvider));
            }
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
                $"Downloading {loader.DownloadCounter} entries took {loader.TimeDownloading / 1000} sec; got {loader.CacheHits} cache hits");
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