using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Itinero.Transit.Belgium;
using Itinero.Transit_Tests;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests
{
    public class TestPcs
    {
        private readonly ITestOutputHelper _output;


        public TestPcs(ITestOutputHelper output)
        {
            _output = output;
        }


        [Fact]
        public void TestIntermodal()
        {
            Log("Starting");
            var deLijn = DeLijn.Profile(ResourcesTest.TestPath, "belgium.routerdb");
            var nmbs = Sncb.Profile(ResourcesTest.TestPath, "belgium.routerdb");

            var profile = new Profile<TransferStats>(
                new ConnectionProviderMerger(nmbs, deLijn),
                new LocationCombiner(nmbs, deLijn),
                nmbs.FootpathTransferGenerator,
                TransferStats.Factory,
                TransferStats.ProfileCompare,
                TransferStats.ParetoCompare
            );
            profile.IntermodalStopSearchRadius = 500;

            var startTime = ResourcesTest.TestMoment(17, 00);
            var endTime = ResourcesTest.TestMoment(19, 30);

            var home = new Uri("https://www.openstreetmap.org/#map=19/51.21576/3.22048");
            var startLocation = OsmLocationMapping.Singleton.GetCoordinateFor(home);
            var starts = deLijn.WalkToCloseByStops(startTime, startLocation, 500);

            var stationGent = new Uri("https://www.openstreetmap.org/#map=16/51.0353/3.7096");
            var endLocationGent = OsmLocationMapping.Singleton.GetCoordinateFor(stationGent);
            var endLocationBrugge = profile.GetCoordinateFor(TestEas.Gent);
            var ends = profile.WalkFromCloseByStops(endTime, endLocationGent, 500);

            

            var pcs = new ProfiledConnectionScan<TransferStats>(
                starts,ends, startTime, endTime, profile);


            var journeys = pcs.CalculateJourneys();
            var found = 0;
            var stats = "";
            TransferStats stat = null;
            var time = TimeSpan.MaxValue;
            foreach (var key in journeys.Keys)
            {
                var journeysFromPtStop = journeys[key];
                foreach (var journey in journeysFromPtStop)
                {
                    Log(journey.ToString(profile));
                    stats += $"{key}: {journey.Stats}\n";
                    stat = journey.Stats;
                    if (time > stat.TravelTime)
                    {
                        time = stat.TravelTime;
                    }
                }

                found += journeysFromPtStop.Count();
            }

            Log($"Got {found} profiles");
            Log(stats);
            Assert.True(found > 0);
            Assert.True(time < new TimeSpan(1,00,00));
        }


        [Fact]
        public void TestProfileScan()
        {
            var sncb = Sncb.Profile(ResourcesTest.TestPath, "belgium.routerdb");
            sncb.IntermodalStopSearchRadius = 0;
            var startTime = ResourcesTest.TestMoment(10, 00);
            var endTime = ResourcesTest.TestMoment(12, 00);
            var pcs = new ProfiledConnectionScan<TransferStats>(
                TestEas.Brugge, TestEas.Gent,
                startTime, endTime, sncb);

            var journeys = new List<Journey<TransferStats>>(
                pcs.CalculateJourneys()[TestEas.Brugge.ToString()]);

            foreach (var j in journeys)
            {
                Log(j.ToString(sncb));
            }

            Assert.Equal(9, journeys.Count);
            Assert.Equal("00:22:00", journeys.ToList()[0].Stats.TravelTime.ToString());
        }


        [Fact]
        public void TestProfileScan2()
        {
            var sncb = Sncb.Profile(ResourcesTest.TestPath, "belgium.routerdb");
            sncb.IntermodalStopSearchRadius = 0;
            var startTime = ResourcesTest.TestMoment(9, 00);
            var endTime = ResourcesTest.TestMoment(20, 00);
            var pcs = new ProfiledConnectionScan<TransferStats>(
                TestEas.Poperinge, TestEas.Vielsalm,
                startTime, endTime, sncb);

            var journeys = new List<Journey<TransferStats>>
                (pcs.CalculateJourneys()[TestEas.Poperinge.ToString()]);

            foreach (var j in journeys)
            {
                Log(
                    $"Journey: {j.Root.Connection.DepartureTime():HH:mm:ss} --> {j.Connection.ArrivalTime():HH:mm:ss}, {j.Stats.NumberOfTransfers} transfers");
            }

            Assert.Equal(6, journeys.Count);
        }

        [Fact]
        public void TestDeLijn()
        {
            Log("Starting");
            var deLijn = DeLijn.Profile(ResourcesTest.TestPath, "belgium.routerdb");
            deLijn.IntermodalStopSearchRadius = 0;
            var startTime = ResourcesTest.TestMoment(16, 00);
            var endTime = ResourcesTest.TestMoment(17, 01);


            var pcs = new ProfiledConnectionScan<TransferStats>(
                TestEas.Howest, TestEas.BruggeStation2, startTime, endTime, deLijn);


            var journeys = pcs.CalculateJourneys();
            var found = 0;
            var stats = "";
            foreach (var key in journeys.Keys)
            {
                var journeysFromPtStop = journeys[key];
                Journey<TransferStats> last = null;
                foreach (var journey in journeysFromPtStop)
                {
                    Log(journey.ToString(deLijn.LocationProvider));
                    stats += $"{key}: {journey.Stats}\n";

                    Assert.Equal(9, (int) (journey.Stats.EndTime - journey.Stats.StartTime).TotalMinutes);
                    Assert.Equal(0, journey.Stats.NumberOfTransfers);
                }

                found += journeysFromPtStop.Count();
            }

            Log($"Got {found} profiles");
            Assert.Equal(17, found);
            Log(stats);
        }


        [Fact]
        public void TestFootPaths()
        {
            Log("Starting");
            var deLijn = DeLijn.Profile(ResourcesTest.TestPath, "belgium.routerdb");
            deLijn.IntermodalStopSearchRadius = 0;
            var startTime = ResourcesTest.TestMoment(16, 00);
            var endTime = ResourcesTest.TestMoment(17, 01);

            var home = new Uri("https://www.openstreetmap.org/#map=19/51.21576/3.22048");
            var startLocation = OsmLocationMapping.Singleton.GetCoordinateFor(home);
            var starts = deLijn.WalkToCloseByStops(startTime, startLocation, 250);

            var station = new Uri("https://www.openstreetmap.org/#map=18/51.19738/3.21830");
            var endLocation = OsmLocationMapping.Singleton.GetCoordinateFor(station);
            var ends = deLijn.WalkFromCloseByStops(endTime, endLocation, 1000);


            var pcs = new ProfiledConnectionScan<TransferStats>(
              starts, //  new List<IContinuousConnection>{new WalkingConnection(TestEas.Howest, startTime)}, 
                ends, startTime, endTime, deLijn);


            var journeys = pcs.CalculateJourneys();
            var found = 0;
            var stats = "";
            TransferStats stat = null;
            foreach (var key in journeys.Keys)
            {
                var journeysFromPtStop = journeys[key];
                foreach (var journey in journeysFromPtStop)
                {
                    Log(journey.ToString(deLijn.LocationProvider));
                    stats += $"{key}: {journey.Stats}\n";
                    stat = journey.Stats;
                }

                found += journeysFromPtStop.Count();
            }

            Log($"Got {found} profiles");
            Log(stats);
            Assert.Equal(15, found);
            Assert.Equal(356, (int) stat.WalkingDistance);
            Assert.Equal(12, (int) (stat.EndTime - stat.StartTime).TotalMinutes);
        }


        [Fact]
        public void TestFootPathsInterlink()
        {
            Program.ConfigureLogging();
            Log("Starting");
            var deLijn = DeLijn.Profile(ResourcesTest.TestPath, "belgium.routerdb");
            // The only difference with the test above:
            deLijn.IntermodalStopSearchRadius = 250;
            var startTime = ResourcesTest.TestMoment(16, 00);
            var endTime = ResourcesTest.TestMoment(17, 01);

            var home = new Uri("https://www.openstreetmap.org/#map=19/51.21576/3.22048");
            var startLocation = OsmLocationMapping.Singleton.GetCoordinateFor(home);
            var starts = deLijn.WalkToCloseByStops(startTime, startLocation, 1000);

            var station = new Uri("https://www.openstreetmap.org/#map=18/51.19738/3.21830");
            var endLocation = OsmLocationMapping.Singleton.GetCoordinateFor(station);
            var ends = deLijn.WalkFromCloseByStops(endTime, endLocation, 1000);


            var pcs = new ProfiledConnectionScan<TransferStats>(
                starts, ends, startTime, endTime, deLijn);


            var journeys = pcs.CalculateJourneys();
            var found = 0;
            var stats = "";
            TransferStats stat = null;
            foreach (var key in journeys.Keys)
            {
                var journeysFromPtStop = journeys[key];
                foreach (var journey in journeysFromPtStop)
                {
                    Log(journey.ToString(deLijn.LocationProvider));
                    stat = journey.Stats;
                    stats += $"{key}: {stat}\n";
                }

                found += journeysFromPtStop.Count();
            }


            Log($"Got {found} profiles");
            Log(stats);
            Assert.Equal(15, found);
            Assert.Equal(356, (int) stat.WalkingDistance);
            Assert.Equal(12, (int) (stat.EndTime - stat.StartTime).TotalMinutes);
        }

        // ReSharper disable once UnusedMember.Local
        private void Log(string s)
        {
            _output.WriteLine(s);
        }
    }
}