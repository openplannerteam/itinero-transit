using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
            var st = new LocalStorage(ResourcesTest.TestPath);
            var deLijn = Belgium.DeLijn(st);
            var nmbs = Belgium.Sncb(st);

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
            var endTime = ResourcesTest.TestMoment(18, 50);

            var home = new Uri("https://www.openstreetmap.org/#map=19/51.21576/3.22048");
            var startLocation = OsmLocationMapping.Singleton.GetCoordinateFor(home);
            var starts = deLijn.WalkToCloseByStops(startTime, startLocation, 500);

            var stationGent = new Uri("https://www.openstreetmap.org/#map=16/51.0353/3.7096");
            var endLocationGent = OsmLocationMapping.Singleton.GetCoordinateFor(stationGent);
            var ends = profile.WalkFromCloseByStops(endTime, endLocationGent, 500);


            var pcs = new ProfiledConnectionScan<TransferStats>(
                starts, ends, startTime, endTime, profile);


            var journeys = pcs.CalculateJourneys();
            var found = 0;
            var stats = "";
            TransferStats stat;
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
            Assert.True(time < new TimeSpan(1, 00, 00));
        }

        [Fact]
        public void TestStationToStation()
        {
            // route?from=&to=&date=121218&time=1230
            var depStation = new Uri("http://irail.be/stations/NMBS/008891009");// Brugge
            var arrival = new Uri("http://irail.be/stations/NMBS/008896735");
            var date = ResourcesTest.TestMoment(12, 30);

            var sncb = Belgium.Sncb(new LocalStorage(ResourcesTest.TestPath));
            var pcs = new ProfiledConnectionScan<TransferStats>(
                depStation, arrival, date, date.AddHours(24), sncb);


            var journeys = pcs.CalculateJourneys()
                .GetValueOrDefault(depStation.ToString(), new List<Journey<TransferStats>>());

            Log(""+journeys.Count());
        }


        [Fact]
        public void TestProfileScan()
        {
            var st = new LocalStorage(ResourcesTest.TestPath);
            var sncb = Belgium.Sncb(st);


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

            Assert.Equal(7, journeys.Count);
            Assert.Equal("00:24:00", journeys.ToList()[0].Stats.TravelTime.ToString());
        }


        [Fact]
        public void TestProfileScan2()
        {
            var st = new LocalStorage(ResourcesTest.TestPath);
            var sncb = Belgium.Sncb(st);


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
            var st = new LocalStorage(ResourcesTest.TestPath);
            var deLijn = Belgium.DeLijn(st);

            deLijn.IntermodalStopSearchRadius = 0;
            var startTime = ResourcesTest.TestMoment(16, 00);
            var endTime = ResourcesTest.TestMoment(17, 01);


            var pcs = new ProfiledConnectionScan<TransferStats>(
                TestEas.Howest, TestEas.BruggeStation2, startTime, endTime, deLijn);


            var journeys = pcs.CalculateJourneys();
            var found = 0;
            var stats = "";
            Journey<TransferStats> last = null;
            foreach (var key in journeys.Keys)
            {
                var journeysFromPtStop = journeys[key];
                foreach (var journey in journeysFromPtStop)
                {
                    Log(journey.ToString(deLijn.LocationProvider));
                    stats += $"{key}: {journey.Stats}\n";

                    var totalTime = (int) (journey.Stats.EndTime - journey.Stats.StartTime).TotalMinutes;
                    Assert.True(totalTime == 9 || totalTime == 13);
                    Assert.True(journey.Stats.NumberOfTransfers <= 1);
                    last = journey;
                }

                found += journeysFromPtStop.Count();
            }

            Log($"Got {found} profiles");
            Log(stats);
            Assert.Equal(8, found);
        }


        [Fact]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public void TestFootPaths()
        {
            Log("Starting");
            var st = new LocalStorage(ResourcesTest.TestPath);
            var deLijn = Belgium.DeLijn(st);


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
            Assert.Equal(9, found);
            Assert.Equal(356, (int) stat.WalkingDistance);
            Assert.Equal(12, (int) (stat.EndTime - stat.StartTime).TotalMinutes);
        }


        [Fact]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public void TestFootPathsInterlink()
        {
            var st = new LocalStorage(ResourcesTest.TestPath);
            var deLijn = Belgium.DeLijn(st);

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
            Assert.Equal(9, found);
            Assert.Equal(356, (int) stat.WalkingDistance);
            Assert.Equal(12, (int) (stat.EndTime - stat.StartTime).TotalMinutes);
        }


        [Fact]
        public void TestTheoretical()
        {
            Log("Starting");
            var test = new TestProfile(new DateTime(2018, 11, 26));
            var prof = test.CreateTestProfile();
            prof.IntermodalStopSearchRadius = 10000;

            var pcs = new ProfiledConnectionScan<TransferStats>(TestProfile.A, TestProfile.D,
                test.Moment(17, 00), test.Moment(19, 01), prof
            );


            var journeys = pcs.CalculateJourneys();


            var found = 0;
            var stats = "";
            foreach (var key in journeys.Keys)
            {
                var journeysFromPtStop = journeys[key];
                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var journey in journeysFromPtStop)
                {
                    Log(journey.ToString(prof));
                    stats += $"{key}: {journey.Stats}\n";
                    Assert.Equal(2, (int) journey.Stats.TravelTime.TotalHours);
                }

                // ReSharper disable once PossibleMultipleEnumeration
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