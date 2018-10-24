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

// ReSharper disable PossibleMultipleEnumeration

// ReSharper disable UnusedMember.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace Itinero_Transit_Tests
{
    public class TestEas
    {
        private readonly ITestOutputHelper _output;

        public static Uri BrusselZuid = LinkedObject.AsUri("http://irail.be/stations/NMBS/008814001");
        public static Uri Gent = LinkedObject.AsUri("http://irail.be/stations/NMBS/008892007");
        public static Uri Brugge = LinkedObject.AsUri("http://irail.be/stations/NMBS/008891009");
        public static Uri Poperinge = LinkedObject.AsUri("http://irail.be/stations/NMBS/008896735");
        public static Uri Vielsalm = LinkedObject.AsUri("http://irail.be/stations/NMBS/008845146");


        public TestEas(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestEarliestArrival()
        {
            var loader = new Downloader();
            // YOU MIGHT HAVE TO SYMLINK THE TIMETABLES TO  Itinero-Transit-Tests/bin/Debug/netcoreapp2.0
            var sncb = new LinkedConnectionProvider(Sncb.HydraSearch(loader));
            var prov = new LocallyCachedConnectionsProvider(sncb,
                new LocalStorage("timetables-for-testing-2018-10-17"));
            var start = new DateTime(2018, 10, 17, 10, 10, 00);
            var timeOut = new DateTime(2018, 10, 17, 23, 0, 0);

            var csa = new EarliestConnectionScan<TransferStats>(
                Brugge, start, Gent, TransferStats.Factory, prov, timeOut);

            var journey = csa.CalculateJourney();
            Log(journey.ToString());
            Assert.Equal("2018-10-17T10:49:00.0000000", $"{journey.Time:O}");
            Assert.Equal("00:39:00", journey.Stats.TravelTime.ToString());
            Assert.Equal(1, journey.Stats.NumberOfTransfers);
        }


        [Fact]
        public void TestEarliestArrival2()
        {
            // YOU MIGHT HAVE TO SYMLINK THE TIMETABLES TO  Itinero-Transit-Tests/bin/Debug/netcoreapp2.0
            var loader = new Downloader();
            var sncb = Sncb.Profile(loader);
            var start = new DateTime(2018, 10, 17, 10, 8, 00);
            var timeOut = new DateTime(2018, 10, 17, 23, 0, 0);
            var csa = new EarliestConnectionScan<TransferStats>(Poperinge, start, Vielsalm, sncb, timeOut);
            var journey = csa.CalculateJourney();
            Log(journey.ToString());

            Assert.Equal("2018-10-17T16:13:00.0000000", $"{journey.Time:O}");
            Assert.Equal("06:05:00", journey.Stats.TravelTime.ToString());
            Assert.Equal(5, journey.Stats.NumberOfTransfers);
        }

        [Fact]
        public void TestDeLijn()
        {
            var loader = new Downloader();
            var storage = new LocalStorage("cache/delijn");
            var deLijn = DeLijn.Profile(loader, storage, "belgium.routerdb");
            Log("Got profile");
            var closeToHome = deLijn.LocationProvider.GetLocationsCloseTo(51.21576f, 3.22f, 250);

            var closeToTarget = deLijn.LocationProvider.GetLocationsCloseTo(51.19738f, 3.21736f, 500);
            Log("Found stops");

            Assert.Equal(6, closeToHome.Count());
            Assert.Equal(16, closeToTarget.Count());

            Assert.True(closeToHome.Contains(new Uri("https://data.delijn.be/stops/502101")));

            var testTime = new DateTime(2018, 10, 24, 10, 00, 00);
            var failOver = new DateTime(2018, 10, 24, 11, 00, 00);

            var startJourneys = new List<Journey<TransferStats>>();
            foreach (var uri in closeToHome)
            {
                startJourneys.Add(new Journey<TransferStats>(uri, testTime, TransferStats.Factory));
                Log($"> {uri} {deLijn.LocationProvider.GetNameOf(uri)}");
            }

            foreach (var uri in closeToTarget)
            {
                Log($"< {uri} {deLijn.LocationProvider.GetNameOf(uri)}");
            }
            
            var eas = new EarliestConnectionScan<TransferStats>(
                startJourneys, new List<Uri>(closeToTarget), deLijn.ConnectionsProvider, failOver);
            Log("Starting AES");
            var j = eas.CalculateJourney();
            Assert.Equal(4, j.Stats.NumberOfTransfers);
            Log("Done");
        }

        private void Log(string s)
        {
            _output.WriteLine(s);
        }
    }
}