namespace Itinero.Transit.Tests.IO.OSM
{
    public class TestRouteCaching
    {
        // TODO: resurrect tests when we reintegrate Itinero.
        
//        private DateTime _tic;
//
//        private void Tic()
//        {
//            _tic = DateTime.Now;
//        }
//
//        private ulong Toc()
//        {
//            return (ulong) (DateTime.Now - _tic).TotalMilliseconds;
//        }
//
//        [Fact]
//        public void TestCache()
//        {
//            var osmStops = new OsmLocationStopReader(0);
//
//            osmStops.MoveTo("https://www.openstreetmap.org/#map=15/51.1893/3.2444"); // Zeven torentjes
//            var stop0 = new Stop(osmStops);
//            osmStops.MoveTo("https://www.openstreetmap.org/#map=19/51.21576/3.22048"); // Elf-Julistraat
//            var stop1 = new Stop(osmStops);
//
//
//            var routerDb = new RouterDb(new RouterDbConfiguration()
//            {
//                Zoom = 14,
//                EdgeDataLayout = new EdgeDataLayout(new (string key, EdgeDataType dataType)[]
//                {
//                    ("bicycle.weight", EdgeDataType.UInt32)
//                })
//            });
//
//
//            routerDb.DataProvider = new DataProvider(routerDb);
//            var osm = new OsmTransferGenerator(routerDb, searchDistance: 50000);
//            var gen = osm.UseCache();
//            Tic();
//            var dist = gen.TimeBetween(stop0, stop1);
//            Assert.NotEqual(uint.MaxValue, dist);
//            var timeNeeded = Toc();
//            Log.Information($"Calculating took {timeNeeded}ms");
//
//            Tic();
//            var dist0 = gen.TimeBetween(stop0, stop1);
//            Assert.NotEqual(uint.MaxValue, dist0);
//            Assert.Equal(dist, dist0);
//            var timeNeeded0 = Toc();
//            Assert.True(timeNeeded0 < 25);
//        }
//        
//        [Fact]
//        public void TestCacheMultiple()
//        {
//            var osmStops = new OsmLocationStopReader(0);
//
//            osmStops.MoveTo("https://www.openstreetmap.org/#map=15/51.1893/3.2444"); // Zeven torentjes
//            var stop0 = new Stop(osmStops);
//            osmStops.MoveTo("https://www.openstreetmap.org/#map=19/51.21576/3.22048"); // Elf-Julistraat
//            var stop1 = new Stop(osmStops);
//            osmStops.MoveTo("https://www.openstreetmap.org/#map=18/51.19738/3.21642"); // Station brugge
//            var stop2 = new Stop(osmStops);
//
//
//            var routerDb = new RouterDb(new RouterDbConfiguration()
//            {
//                Zoom = 14,
//                EdgeDataLayout = new EdgeDataLayout(new (string key, EdgeDataType dataType)[]
//                {
//                    ("bicycle.weight", EdgeDataType.UInt32)
//                })
//            });
//
//
//            routerDb.DataProvider = new DataProvider(routerDb);
//            var osm = new OsmTransferGenerator(routerDb, searchDistance: 50000);
//            var gen = osm.UseCache();
//            Tic();
//            var dist = gen.TimesBetween(stop0, new List<IStop>{stop1,stop2});
//            var timeNeeded = Toc();
//            Assert.False(dist.ContainsValue(uint.MaxValue));
//            Log.Information($"Calculating took {timeNeeded}ms");
//
//            Tic();
//            var dist0 = gen.TimesBetween(stop0, new List<IStop>{stop1,stop2});
//            var timeNeeded0 = Toc();
//            Assert.Equal(dist, dist0);
//            Assert.True(timeNeeded0 < 25);
//        }
    }
}