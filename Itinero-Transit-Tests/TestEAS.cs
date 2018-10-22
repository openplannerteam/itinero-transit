using System;
using Itinero_Transit.CSA;
using Itinero_Transit.CSA.ConnectionProviders;
using Itinero_Transit.CSA.Data;
using Itinero_Transit.LinkedData;
using Xunit;
using Xunit.Abstractions;

namespace Itinero_Transit_Tests
{
    public class TestEAS
    {
        private readonly ITestOutputHelper _output;

        public static Uri brusselZuid = LinkedObject.AsUri("http://irail.be/stations/NMBS/008814001");
        public static Uri gent = LinkedObject.AsUri("http://irail.be/stations/NMBS/008892007");
        public static Uri brugge = LinkedObject.AsUri("http://irail.be/stations/NMBS/008891009");
        public static Uri poperinge = LinkedObject.AsUri("http://irail.be/stations/NMBS/008896735");
        public static Uri vielsalm = LinkedObject.AsUri("http://irail.be/stations/NMBS/008845146");


        public TestEAS(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestEarliestArrival()
        {
            // YOU MIGHT HAVE TO SYMLINK THE TIMETABLES TO  Itinero-Transit-Tests/bin/Debug/netcoreapp2.0

            var prov = new LocallyCachedConnectionsProvider(new SncbConnectionProvider(),
                new LocalStorage("timetables-for-testing-2018-10-17"));
            var start = new DateTime(2018, 10, 17, 10, 10, 00);
            var timeOut = new DateTime(2018, 10, 17, 23, 0, 0);

            var csa = new EarliestConnectionScan<TransferStats>(
                brugge, start, gent, TransferStats.Factory, prov, timeOut);

            var journey = csa.CalculateJourney();
            Log(journey.ToString());
            Assert.Equal("2018-10-17T10:36:00.0000000", $"{journey.Time:O}");
            Assert.Equal("00:26:00", journey.Stats.TravelTime.ToString());
            Assert.Equal(0, journey.Stats.NumberOfTransfers);
        }


        [Fact]
        public void TestEarliestArrival2()
        {
            // YOU MIGHT HAVE TO SYMLINK THE TIMETABLES TO  Itinero-Transit-Tests/bin/Debug/netcoreapp2.0

            var prov = new LocallyCachedConnectionsProvider(new SncbConnectionProvider(),
                new LocalStorage("timetables-for-testing-2018-10-17"));
            var start = new DateTime(2018, 10, 17, 10, 8, 00);
            var timeOut = new DateTime(2018, 10, 17, 23, 0, 0);
            var csa = new EarliestConnectionScan<TransferStats>(poperinge, start, vielsalm, TransferStats.Factory,
                prov, timeOut);
            var journey = csa.CalculateJourney();
            Log(journey.ToString());

            Assert.Equal("2018-10-17T15:13:00.0000000", $"{journey.Time:O}");
            Assert.Equal("05:05:00", journey.Stats.TravelTime.ToString());
            Assert.Equal(5, journey.Stats.NumberOfTransfers);
        }

        private void Log(string s)
        {
            _output.WriteLine(s);
        }
    }
}