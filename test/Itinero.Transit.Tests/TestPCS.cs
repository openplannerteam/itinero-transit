using System;
using System.Collections.Generic;
using System.Linq;
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
                TransferStats.ProfileTransferCompare,
                TransferStats.ParetoCompare
            );
            profile.IntermodalStopSearchRadius = 100;
            
            var startTime = ResourcesTest.TestMoment(17, 00);
            var endTime = ResourcesTest.TestMoment(17, 31);
           
            var home = new Uri("https://www.openstreetmap.org/#map=19/51.21576/3.22048");
            var startLocation = OsmLocationMapping.Singleton.GetCoordinateFor(home);
            var starts = deLijn.WalkToCloseByStops(startTime, startLocation, 1000);

            var station = new Uri("https://www.openstreetmap.org/#map=16/51.0353/3.7096");
            var endLocation = profile.GetCoordinateFor(TestEas.Brugge);
                // OsmLocationMapping.Singleton.GetCoordinateFor(station);
            var ends = profile.WalkFromCloseByStops(endTime, endLocation, 1000);


            var pcs = new ProfiledConnectionScan<TransferStats>(
                TestEas.Howest, TestEas.BruggeStation2, startTime, endTime, profile);


            var journeys = pcs.CalculateJourneys();
            var found = 0;
            var stats = "";
            TransferStats stat = null;
            foreach (var key in journeys.Keys)
            {
                var journeysFromPtStop = journeys[key];
                foreach (var journey in journeysFromPtStop)
                {
                    Log(journey.ToString(profile));
                    stats += $"{key}: {journey.Stats}\n";
                    stat = journey.Stats;
                }

                found += journeysFromPtStop.Count();
            }

            Log($"Got {found} profiles");
            Log(stats);

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

            Assert.Equal(10, journeys.Count);
            Assert.Equal("00:22:00", journeys.ToList()[0].Stats.TravelTime.ToString());
        }


        [Fact]
        public void TestProfileScan2()
        {
            var sncb = Sncb.Profile(ResourcesTest.TestPath, "belgium.routerdb");
            sncb.IntermodalStopSearchRadius = 0;
            var startTime = ResourcesTest.TestMoment(10, 00);
            var endTime = ResourcesTest.TestMoment(20, 00);
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
            var deLijn = DeLijn.Profile(ResourcesTest.TestPath, "belgium.routerdb");
            deLijn.IntermodalStopSearchRadius = 0;
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
                    stats += $"{key}: {journey.Stats}\n";
                    stat = journey.Stats;
                }

                found += journeysFromPtStop.Count();
            }

            Log($"Got {found} profiles");
            Log(stats);
            Assert.Equal(2, found);
            Assert.Equal(1353,(int) stat.WalkingDistance);
            Assert.Equal(32,(int) (stat.EndTime - stat.StartTime).TotalMinutes);

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
            Assert.Equal(3, found);
            Assert.Equal(401,(int) stat.WalkingDistance);
            Assert.Equal(19,(int) (stat.EndTime - stat.StartTime).TotalMinutes);
        }
        
        // ReSharper disable once UnusedMember.Local
        private void Log(string s)
        {
            _output.WriteLine(s);
        }
    }
}