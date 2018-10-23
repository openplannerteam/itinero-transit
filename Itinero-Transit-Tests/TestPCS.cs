using System;
using System.Linq;
using Itinero_Transit.CSA;
using Itinero_Transit.CSA.ConnectionProviders;
using Itinero_Transit.CSA.Data;
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
            var pcs = new ProfiledConnectionScan<TransferStats>(TestEas.Brugge, TestEas.Gent,
                Sncb.Profile(loader, storage, "belgium.routerdb"));

            var journeys = pcs.CalculateJourneys(new DateTime(2018, 10, 17, 10, 00, 00),
                new DateTime(2018, 10, 17, 12, 00, 00));

            Assert.Equal(2, journeys.Count);
            Assert.Equal("00:22:00", journeys.ToList()[0].Stats.TravelTime.ToString());
        }


        [Fact]
        public void TestProfileScan2()
        {
            // YOU MIGHT HAVE TO SYMLINK THE TIMETABLES TO  Itinero-Transit-Tests/bin/Debug/netcoreapp2.0
            var loader = new Downloader();

            var storage = new LocalStorage("timetables-for-testing-2018-10-17");
            var pcs = new ProfiledConnectionScan<TransferStats>(
                TestEas.Poperinge, TestEas.Vielsalm,
                Sncb.Profile(loader, storage, "belgium.routerdb"));

            var journeys = pcs.CalculateJourneys(new DateTime(2018, 10, 17, 10, 00, 00),
                new DateTime(2018, 10, 17, 20, 00, 00));
            foreach (var j in journeys)
            {
                Log(
                    $"Journey: {j.Connection.DepartureTime():HH:mm:ss} --> {j.First().Connection.ArrivalTime():HH:mm:ss}, {j.Stats.NumberOfTransfers} transfers");
            }

            Assert.Equal(3, journeys.Count);
        }

        // ReSharper disable once UnusedMember.Local
        private void Log(string s)
        {
            _output.WriteLine(s);
        }
    }
}