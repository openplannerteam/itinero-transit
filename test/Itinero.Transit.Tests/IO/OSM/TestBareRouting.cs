using System.IO;
using Itinero.IO.Osm.Tiles;
using Itinero.Profiles.Lua;
using Itinero.Profiles.Lua.Osm;
using Itinero.Transit.IO.OSM;
using Xunit;

namespace Itinero.Transit.Tests.IO.OSM
{
    public class TestBareRouting
    {
        [Fact]
        public void TestNoBoilerplateEBike()
        {
            var routerDb = new RouterDb();
            routerDb.DataProvider = new DataProvider(routerDb);

            var bicycle = LuaProfile.Load(File.ReadAllText(@"ebike.lua"));

            var sp1 = routerDb.Snap(3.218109999999996, 51.21459999999999, profile: bicycle);
            var sp2 = routerDb.Snap(3.2167249917984009, 51.197229555160746, profile: bicycle);
            var config = new RoutingSettings
            {
                Profile =  bicycle,
                MaxDistance = 2500
            };
            var route = routerDb.Calculate(config, sp1, sp2);

            Assert.False(route.IsError);
            Assert.True(route.Value.Shape.Count > 10);
        }

        [Fact]
        public void TestNoBoilerplatePedestrian()
        {
            var routerDb = new RouterDb();
            routerDb.DataProvider = new DataProvider(routerDb);

            var profile = OsmProfiles.Pedestrian;

            var sp1 = routerDb.Snap(3.218109999999996, 51.21459999999999, profile: profile);
            var sp2 = routerDb.Snap(3.2167249917984009, 51.197229555160746, profile: profile);
            Assert.True(!sp1.IsError);
            Assert.True(!sp2.IsError);
            var config = new RoutingSettings
            {
                Profile =  profile,
                MaxDistance = 5000
            };
            var route = routerDb.Calculate(config, sp1, sp2);
            Assert.NotNull(route);
            Assert.True(!route.IsError);
            Assert.True(route.Value.Shape.Count > 10);
        }


        [Fact]
        public void TestRijselsestraatBrugge2Station()
        {
            var routerDb = new RouterDb();
            routerDb.DataProvider = new DataProvider(routerDb);

            var pedestrian = OsmProfiles.Pedestrian;

            (double lat, double lon) from = (51.21459999999999, 3.218109999999996);
            (double lat, double lon) to = (51.197229555160746, 3.2167249917984009);


            // TODO: this should not be in the unit tests, it uses the web to load data.
            var p = new OsmTransferGenerator(routerDb, 5000, pedestrian);
            // Rijselstraat, just behind the station
            var route0 = p.CreateRoute(from, to, out _, out _);
            Assert.NotNull(route0);
            Assert.True(route0.Shape.Count > 1);
        }
    }
}