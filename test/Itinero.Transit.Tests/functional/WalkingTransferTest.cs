using System;
using Itinero.IO.LC;
using Itinero.Transit;
using Itinero.Transit.Tests;
using Xunit;
using Xunit.Abstractions;
// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.IO.LC.Tests
{
    public class WalkingTransferTest : SuperTest
    {
        public static readonly Uri Howest = new Uri("https://data.delijn.be/stops/502132");
        public static readonly Uri Ezelspoort = new Uri("https://data.delijn.be/stops/502102");


        public WalkingTransferTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TestCreateRoute()
        {
            var st = new LocalStorage(ResourcesTest.TestPath);
            var deLijn = Belgium.DeLijn(st);

            Log("Creating WCP");
            var wcp = new OsmTransferGenerator("belgium.routerdb");

            DateTime start = DateTime.Now;
            var wt = wcp.GenerateFootPaths(start, deLijn.GetCoordinateFor(Howest),
                deLijn.GetCoordinateFor(Ezelspoort));
            Log(wt.ToString());
            Assert.Equal(565, (int) (wt.ArrivalTime() - wt.DepartureTime()).TotalSeconds);
        }
    }
}