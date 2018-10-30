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

        public static Uri BrusselZuid = new Uri("http://irail.be/stations/NMBS/008814001");
        public static Uri Gent = new Uri("http://irail.be/stations/NMBS/008892007");
        public static Uri Brugge = new Uri("http://irail.be/stations/NMBS/008891009");
        public static Uri Poperinge = new Uri("http://irail.be/stations/NMBS/008896735");
        public static Uri Vielsalm = new Uri("http://irail.be/stations/NMBS/008845146");


        public TestEas(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestEarliestArrival()
        {
            // YOU MIGHT HAVE TO SYMLINK THE TIMETABLES TO  Itinero-Transit-Tests/bin/Debug/netcoreapp2.0
            var loader = new Downloader();
            var storage = new LocalStorage("timetables-for-testing-2018-10-17");
            var sncb = Sncb.Profile(loader, storage, "belgium.routerdb");
            var startTime = new DateTime(2018, 10, 17, 10, 10, 00);
            var endTime = new DateTime(2018, 10, 17, 23, 0, 0);

            var csa = new EarliestConnectionScan<TransferStats>(Brugge, Gent, startTime, endTime, sncb);

            var journey = csa.CalculateJourney();
            Log(journey.ToString());
            Assert.Equal("2018-10-17T10:24:00.0000000", $"{journey.Connection.DepartureTime():O}");
            Assert.Equal("00:26:00", journey.Stats.TravelTime.ToString());
            Assert.Equal(0, journey.Stats.NumberOfTransfers);
        }


        [Fact]
        public void TestEarliestArrival2()
        {
            // YOU MIGHT HAVE TO SYMLINK THE TIMETABLES TO  Itinero-Transit-Tests/bin/Debug/netcoreapp2.0
            var loader = new Downloader();
            var storage = new LocalStorage("timetables-for-testing-2018-10-17");
            var sncb = Sncb.Profile(loader, storage, "belgium.routerdb");
           
            var startTime = new DateTime(2018, 10, 17, 10, 8, 00);
            var endTime = new DateTime(2018, 10, 17, 23, 0, 0);
            var csa = new EarliestConnectionScan<TransferStats>(
                Poperinge, Vielsalm, startTime, endTime, sncb);
            var journey = csa.CalculateJourney();
            Log(journey.ToString());

            Assert.Equal("2018-10-17T15:01:00.0000000", $"{journey.Connection.DepartureTime():O}");
            Assert.Equal("05:05:00", journey.Stats.TravelTime.ToString());
            Assert.Equal(3, journey.Stats.NumberOfTransfers);
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

            var startTime = new DateTime(2018, 10, 24, 10, 00, 00);
            var endTime = new DateTime(2018, 10, 24, 11, 00, 00);

            var startJourneys = new List<Journey<TransferStats>>();
            foreach (var uri in closeToHome)
            {
                startJourneys.Add(new Journey<TransferStats>(uri, startTime, TransferStats.Factory));
                Log($"> {uri} {deLijn.LocationProvider.GetNameOf(uri)}");
            }

            foreach (var uri in closeToTarget)
            {
                Log($"< {uri} {deLijn.LocationProvider.GetNameOf(uri)}");
            }
            
            var eas = new EarliestConnectionScan<TransferStats>(
                startJourneys, new List<Uri>(closeToTarget),
                deLijn, endTime);
            Log("Starting AES");
            var j = eas.CalculateJourney();
            Log(j.ToString(deLijn));
            Assert.Equal(0, j.Stats.NumberOfTransfers);
            Assert.Equal(7, (j.Stats.EndTime - j.Stats.StartTime).TotalMinutes);
            Log("Done");
        }

        private void Log(string s)
        {
            _output.WriteLine(s);
        }
    }
}