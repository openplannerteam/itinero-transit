//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Itinero.IO.LC;
//using Itinero.Transit.IO.LC.CSA;
//using Itinero.Transit.IO.LC.CSA.Stats;
//using Itinero.Transit.IO.LC.CSA.Utils;
//using Itinero.Transit_Tests;
//using Xunit;
//using Xunit.Abstractions;
//
//// ReSharper disable FieldCanBeMadeReadOnly.Global
//// ReSharper disable UnusedMember.Global
//// ReSharper disable PossibleMultipleEnumeration
//
//namespace Itinero.IO.LC.Tests
//{
//    public class TestEas
//    {
//        private readonly ITestOutputHelper _output;
//
//        public static Uri BrusselZuid = new Uri("http://irail.be/stations/NMBS/008814001");
//        public static Uri Gent = new Uri("http://irail.be/stations/NMBS/008892007");
//        public static Uri Brugge = new Uri("http://irail.be/stations/NMBS/008891009");
//        public static Uri Poperinge = new Uri("http://irail.be/stations/NMBS/008896735");
//        public static Uri Vielsalm = new Uri("http://irail.be/stations/NMBS/008845146");
//
//        public static Uri Howest = new Uri("https://data.delijn.be/stops/502132");
//        public static Uri BruggeStation2 = new Uri("https://data.delijn.be/stops/500042");
//        public static Uri BruggeNearStation = new Uri("https://data.delijn.be/stops/507076");
//
//        public TestEas(ITestOutputHelper output)
//        {
//            _output = output;
//        }
//
//
//        [Fact]
//        public void TestIntermodalEas()
//        {
//            var st = new LocalStorage(ResourcesTest.TestPath);
//            var nmbs = Belgium.Sncb(st);
//            var delijn = Belgium.DeLijn(st);
//
//            var profile = new Profile<TransferStats>(
//                new ConnectionProviderMerger(nmbs, delijn),
//                new LocationCombiner(nmbs, delijn),
//                nmbs.FootpathTransferGenerator,
//                TransferStats.Factory);
//
//            var startTime = ResourcesTest.TestMoment(10, 0);
//            var endTime = ResourcesTest.TestMoment(14, 00);
//
//            var eas = new EarliestConnectionScan<TransferStats>(
//                Howest, Gent,
//                startTime, endTime,
//                profile
//            );
//
//
//            var journey = eas.CalculateJourney();
//            Log(journey.ToString(profile));
//
//            Log(journey.AsRoute(profile).ToGeoJson());
//
//            Assert.True(ResourcesTest.TestMoment(11, 14) > journey.Connection.ArrivalTime());
//            Assert.True(2 >= journey.Stats.NumberOfTransfers);
//        }
//
//
//        [Fact]
//        public void TestEarliestArrival()
//        {
//            var st = new LocalStorage(ResourcesTest.TestPath);
//            var sncb = Belgium.Sncb(st);
//            var startTime = ResourcesTest.TestMoment(10, 10);
//            var endTime = ResourcesTest.TestMoment(23, 00);
//
//            var csa = new EarliestConnectionScan<TransferStats>(Brugge, Gent, startTime, endTime, sncb);
//
//            var journey = csa.CalculateJourney();
//            Log(journey.ToString());
//            Assert.True(1 >= journey.Stats.NumberOfTransfers);
//        }
//
//
//        [Fact]
//        public void TestEarliestArrival2()
//        {
//            var st = new LocalStorage(ResourcesTest.TestPath);
//            var sncb = Belgium.Sncb(st);
//
//            var startTime = ResourcesTest.TestMoment(11, 08);
//            var endTime = ResourcesTest.TestMoment(23, 00);
//            var csa = new EarliestConnectionScan<TransferStats>(
//                Poperinge, Vielsalm, startTime, endTime, sncb);
//            var journey = csa.CalculateJourney();
//            Log(journey.ToString(sncb));
//
//            Assert.True(ResourcesTest.TestMoment(17, 01) > journey.Connection.DepartureTime());
//            Assert.True(6 > journey.Stats.TravelTime.TotalHours);
//            Assert.True(5 >= journey.Stats.NumberOfTransfers);
//        }
//
//        [Fact]
//        public void TestDeLijn()
//        {
//            var st = new LocalStorage(ResourcesTest.TestPath);
//            var deLijn = Belgium.DeLijn(st);
//
//            Log("Got profile");
//            var closeToHome = deLijn.LocationProvider.GetLocationsCloseTo(51.21576f, 3.22f, 250);
//
//            var closeToTarget = deLijn.LocationProvider.GetLocationsCloseTo(51.19738f, 3.21736f, 500);
//            Log("Found stops");
//
//            Assert.Equal(6, closeToHome.Count());
//            Assert.Equal(16, closeToTarget.Count());
//
//            Assert.Contains(new Uri("https://data.delijn.be/stops/502101"), closeToHome);
//
//            var startTime = ResourcesTest.TestMoment(10, 00);
//            var endTime = ResourcesTest.TestMoment(11, 00);
//
//            var startJourneys = new List<Journey<TransferStats>>();
//            foreach (var uri in closeToHome)
//            {
//                startJourneys.Add(new Journey<TransferStats>(uri, startTime, TransferStats.Factory));
//                Log($"> {uri} {deLijn.LocationProvider.GetNameOf(uri)}");
//            }
//
//            foreach (var uri in closeToTarget)
//            {
//                Log($"< {uri} {deLijn.LocationProvider.GetNameOf(uri)}");
//            }
//
//            var eas = new EarliestConnectionScan<TransferStats>(
//                startJourneys, new List<Uri>(closeToTarget),
//                deLijn, endTime);
//            Log("Starting AES");
//            var j = eas.CalculateJourney();
//            Log(j.ToString(deLijn));
//            Assert.Equal(0, j.Stats.NumberOfTransfers);
//            Assert.Equal(7, (j.Stats.EndTime - j.Stats.StartTime).TotalMinutes);
//            Log("Done");
//        }
//
//        private void Log(string s)
//        {
//            _output.WriteLine(s);
//        }
//    }
//}