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

        private Uri brusselZuid = LinkedObject.AsUri("http://irail.be/stations/NMBS/008814001");
        private Uri gent = LinkedObject.AsUri("https://irail.be/stations/NMBS/008892007");
        private Uri brugge = LinkedObject.AsUri("https://irail.be/stations/NMBS/008891009");
        private Uri poperinge = LinkedObject.AsUri("https://irail.be/stations/NMBS/008896735");
        private Uri vielsalm = LinkedObject.AsUri("https://irail.be/stations/NMBS/008845146");


        public TestPcs(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestProfileScan()
        {
            // YOU MIGHT HAVE TO SYMLINK THE TIMETABLES TO  Itinero-Transit-Tests/bin/Debug/netcoreapp2.0

            Downloader.AlwaysReturn = "<downloading disabled for test>";

            var prov = new LocallyCachedConnectionsProvider(new SncbConnectionProvider(),
                new LocalStorage("timetables-for-testing-2018-10-02"));
            var pcs = new ProfiledConnectionScan<TransferStats>(brugge, gent, prov, TransferStats.Factory, TransferStats.ProfileCompare);

            var journeys = pcs.CalculateJourneys(new DateTime(2018, 10, 02, 10, 00, 00), new DateTime(2018,10,02,12,00,00));

            Assert.Equal(9, journeys.Count());
            Assert.Equal("00:22:00", journeys[0].Stats.TravelTime.ToString());
            
            
            var filter=  new ParetoFrontier<TransferStats>(TransferStats.ParetoCompare);
            journeys = filter.ParetoFront(journeys);

            Assert.Equal(2, journeys.Count());
            Assert.Equal("00:22:00", journeys[0].Stats.TravelTime.ToString());



        }
        
        
        [Fact]
        public void TestProfileScan2()
        {
            // YOU MIGHT HAVE TO SYMLINK THE TIMETABLES TO  Itinero-Transit-Tests/bin/Debug/netcoreapp2.0

            Downloader.AlwaysReturn = "<downloading disabled for test>";

            var prov = new LocallyCachedConnectionsProvider(new SncbConnectionProvider(),
                new LocalStorage("timetables-for-testing-2018-10-02"));
            var pcs = new ProfiledConnectionScan<TransferStats>(poperinge, vielsalm, prov, TransferStats.Factory, TransferStats.ProfileCompare);

            var journeys = pcs.CalculateJourneys(new DateTime(2018, 10, 02, 10, 00, 00), new DateTime(2018,10,02,20,00,00));
            Assert.Equal(5, journeys.Count); 

        }

        private void Log(string s)
        {
            _output.WriteLine(s);
        }
    }
}