
using Itinero.IO.Osm.Tiles;
using Itinero.Profiles.Lua.Osm;
using Itinero.Transit.Tests.Functional.Transfers;
using Xunit;

namespace Itinero.Transit.Tests.IO.OSM
{
    public class TestBareRouting
    {
        [Fact]
        public void TestRijselsestraatBrugge2Station()
        {
            var routerDb = new RouterDb {DataProvider = new DataProvider()};

            var pedestrian = OsmProfiles.Pedestrian;

            (double lat, double lon) from = (51.21459999999999, 3.218109999999996);
            (double lat, double lon) to = (51.197229555160746, 3.2167249917984009);


            // Note: this uses the web to load data.
            var p = new OsmTransferGenerator(routerDb, 5000, pedestrian);
            // Rijselstraat, just behind the station
            var route0 = p.CreateRoute(from, to, out _, out _);
            Assert.NotNull(route0);
            Assert.True(route0.Shape.Count > 1);
        }
    }
}