using System;
using System.IO;
using Itinero.IO.Json;
using Itinero.IO.Osm.Tiles;
using Itinero.Profiles.Lua;
using Itinero.Profiles.Lua.Osm;
using Itinero.Transit.Data;
using Itinero.Transit.IO.OSM;
using Itinero.Transit.OtherMode;
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
            var route = routerDb.Calculate(bicycle, sp1, sp2);

            Assert.False(route.IsError);
            Assert.True(route.Value.Shape.Count > 10);
        }

        [Fact]
        public void TestNoBoilerplatePedestrian()
        {
            var routerDb = new RouterDb();
            routerDb.DataProvider = new DataProvider(routerDb);

            var bicycle = OsmProfiles.Pedestrian;

            var sp1 = routerDb.Snap(3.218109999999996, 51.21459999999999, profile: bicycle);
            var sp2 = routerDb.Snap(3.2167249917984009, 51.197229555160746, profile: bicycle);
            var route = routerDb.Calculate(bicycle, sp1, sp2);
            Assert.NotNull(route);
            Assert.True(route.Value.Shape.Count > 10);
        }

        [Fact]
        public void TestBoilerPlate()
        {
            var routerDb = new RouterDb();
            routerDb.DataProvider = new DataProvider(routerDb);

            var pedestrian = OsmProfiles.Pedestrian;


            var p = new OsmTransferGenerator(5000, profile: pedestrian);
            // HOWEST
            var from = new Stop("a", new LocationId(0, 0, 0),
                3.218109999999996, 51.21459999999999, null);
            // STATION BRUGGE
            var to = new Stop("b", new LocationId(0, 0, 1),
                3.2167249917984009, 51.197229555160746, null);


            var route = p.CreateRoute(from, to, out _);
            Assert.NotNull(route);
            Assert.True(route.Shape.Count > 10);
        }

        [Fact]
        public void TestRijselsestraatBrugge2Station()
        {
            var routerDb = new RouterDb();
            routerDb.DataProvider = new DataProvider(routerDb);

            var pedestrian = OsmProfiles.Pedestrian;


            var p = new OsmTransferGenerator(5000, profile: pedestrian);
            // Rijselstraat, just behind the station
            var from = new Stop("a", new LocationId(0, 0, 0),
                3.2137800000000141, 51.193350000000009, null);
            var to = new Stop("b", new LocationId(0, 0, 1),
                3.2167249917984009, 51.197229555160746, null);

            var t = p.UseCache().TimeBetween(from, to);
            Assert.True(t > 0 && t != uint.MaxValue);
            var route = p.CreateRoute(from, to, out _);
            Assert.NotNull(route);
            Assert.True(route.Shape.Count > 10);
        }
    }
}