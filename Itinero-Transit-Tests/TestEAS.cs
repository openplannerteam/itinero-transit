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

        private Uri brusselZuid = LinkedObject.AsUri("http://irail.be/stations/NMBS/008814001");
        private Uri gent = LinkedObject.AsUri("https://irail.be/stations/NMBS/008892007");
        private Uri brugge = LinkedObject.AsUri("https://irail.be/stations/NMBS/008891009");
        private Uri poperinge = LinkedObject.AsUri("https://irail.be/stations/NMBS/008896735");
        private Uri vielsalm = LinkedObject.AsUri("https://irail.be/stations/NMBS/008845146");


        public TestEAS(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestEarliestArrival()
        {
            // YOU MIGHT HAVE TO SYMLINK THE TIMETABLES TO  Itinero-Transit-Tests/bin/Debug/netcoreapp2.0

            Downloader.AlwaysReturn = "<downloading disabled for test>";

            var prov = new LocallyCachedConnectionsProvider(new SncbConnectionProvider(),
                new LocalStorage("timetables-for-testing-2018-10-02"));
            var csa = new EarliestConnectionScan<TransferStats>(brugge, gent, TransferStats.Factory, prov);

            var journey = csa.CalculateJourney(new DateTime(2018, 10, 02, 10, 00, 00));
            Log(journey.ToString());
        }

        // ReSharper disable once UnusedMember.Local
        private void Log(string s)
        {
            _output.WriteLine(s);
        }
    }
}