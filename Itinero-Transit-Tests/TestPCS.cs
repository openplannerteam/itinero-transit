using System;
using System.Collections.Generic;
using System.Linq;
using Itinero_Transit.CSA;
using Itinero_Transit.CSA.ConnectionProviders;
using Itinero_Transit.CSA.Data;
using Itinero_Transit.CSA.LocationProviders;
using Itinero_Transit.LinkedData;
using Xunit;
using Xunit.Abstractions;

namespace Itinero_Transit_Tests
{
    public class TestPcs
    {
        private readonly ITestOutputHelper _output;


        public TestPcs(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestProfileScan()
        {
            // YOU MIGHT HAVE TO SYMLINK THE TIMETABLES TO  Itinero-Transit-Tests/bin/Debug/netcoreapp2.0
            var loader = new Downloader();
            var storage = new LocalStorage("timetables-for-testing-2018-10-17");
            var sncb = Sncb.Profile(loader, storage, "belgium.routerdb");
            sncb.IntermodalStopSearchRadius = 0;
            var startTime = new DateTime(2018, 10, 17, 10, 00, 00);
            var endTime = new DateTime(2018, 10, 17, 12, 00, 00);
            var pcs = new ProfiledConnectionScan<TransferStats>(
                TestEas.Brugge, TestEas.Gent,
                startTime, endTime, sncb);

            var journeys = new List<Journey<TransferStats>>(
                pcs.CalculateJourneys()[TestEas.Brugge.ToString()]);

            Assert.Equal(10, journeys.Count);
            Assert.Equal("00:22:00", journeys.ToList()[0].Stats.TravelTime.ToString());
        }


        [Fact]
        public void TestProfileScan2()
        {
            // YOU MIGHT HAVE TO SYMLINK THE TIMETABLES TO  Itinero-Transit-Tests/bin/Debug/netcoreapp2.0
            var loader = new Downloader();
            var storage = new LocalStorage("timetables-for-testing-2018-10-17");
            var sncb = Sncb.Profile(loader, storage, "belgium.routerdb");
            sncb.IntermodalStopSearchRadius = 0;
            var startTime = new DateTime(2018, 10, 17, 10, 00, 00);
            var endTime = new DateTime(2018, 10, 17, 20, 00, 00);
            var pcs = new ProfiledConnectionScan<TransferStats>(
                TestEas.Poperinge, TestEas.Vielsalm,
                startTime, endTime, sncb);

            var journeys = new List<Journey<TransferStats>>
                (pcs.CalculateJourneys()[TestEas.Poperinge.ToString()]);

            foreach (var j in journeys)
            {
                Log(
                    $"Journey: {j.Connection.DepartureTime():HH:mm:ss} --> {j.First().Connection.ArrivalTime():HH:mm:ss}, {j.Stats.NumberOfTransfers} transfers");
            }

            Assert.Equal(5, journeys.Count);
        }


        [Fact]
        public void TestFootPaths()
        {
            Log("Starting");
            var loader = new Downloader();
            var storage = new LocalStorage("cache/delijn");
            var deLijn = DeLijn.Profile(loader, storage, "belgium.routerdb");
            deLijn.IntermodalStopSearchRadius = 0;
            var startTime = new DateTime(2018, 10, 30, 16, 00, 00);
            var endTime = new DateTime(2018, 10, 30, 17, 00, 00);
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
                    Log(journey.ToString(deLijn.LocationProvider));
                    stats += $"{key}: {journey.Stats}\n";
                }

                found += journeysFromPtStop.Count();
            }

            Log($"Got {found} profiles");
            Log(stats);
        }

        [Fact]
        public void TestFootPathsInterlink()
        {
            Assert.Equal("unsupported","timeout");
            Log("Starting");
            var loader = new Downloader();
            var storage = new LocalStorage("cache/delijn");
            var deLijn = DeLijn.Profile(loader, storage, "belgium.routerdb");
            
            deLijn.IntermodalStopSearchRadius =100;
            
            var startTime = new DateTime(2018, 10, 30, 16, 00, 00);
            var endTime = new DateTime(2018, 10, 30, 17, 00, 00);
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
                    Log(journey.ToString(deLijn.LocationProvider));
                    stats += $"{key}: {journey.Stats}\n";
                }

                found += journeysFromPtStop.Count();
            }

            Log($"Got {found} profiles");
            Log(stats);
        }
        
        // ReSharper disable once UnusedMember.Local
        private void Log(string s)
        {
            _output.WriteLine(s);
        }
    }
}