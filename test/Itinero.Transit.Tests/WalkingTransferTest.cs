using System;
using Itinero.Transit.CSA.ConnectionProviders;
using Itinero.Transit.CSA.Connections;
using Itinero.Transit.CSA.Data;
using Itinero.Transit.LinkedData;
using Xunit;
using Xunit.Abstractions;
// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit_Tests
{
    public class WalkingTransferTest
    {
        private readonly ITestOutputHelper _output;
        public static readonly Uri Howest = new Uri("https://data.delijn.be/stops/502132");
        public static readonly Uri Ezelspoort = new Uri("https://data.delijn.be/stops/502102");

        public WalkingTransferTest(ITestOutputHelper output)
        {
            _output = output;
        }

        // ReSharper disable once UnusedMember.Local
        private void Log(string s)
        {
            _output.WriteLine(s);
        }

        [Fact]
        public void TestCreateRoute()
        {
            var loader = new Downloader();
            var deLijn = DeLijn.LocationProvider(loader, new LocalStorage("DeLijn"));
            Log("Creating WCP");
            var wcp = new TransferGenerator("belgium.routerdb");

            DateTime start = DateTime.Now;
            var wt = wcp.GenerateFootPaths(start, deLijn.GetCoordinateFor(Howest),
                deLijn.GetCoordinateFor(Ezelspoort));
            Log(wt.ToString());
            Assert.Equal(565, (int) (wt.ArrivalTime() - wt.DepartureTime()).TotalSeconds);
        }
    }
}