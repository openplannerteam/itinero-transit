using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.IO.OSM.Data;
using Xunit;

namespace Itinero.Transit.Tests.Core.Data
{
    public class StopSearchCacherTest
    {
        [Fact]
        public void MakeComplete_FixedStops_AllStopsAreKnown()
        {
            var osmReader = new OsmLocationStopReader(0, hoard: true);
            var stationBrugge = "https://www.openstreetmap.org/#map=19/51.19714/3.21795";
            var stationBruggeExpected = "https://www.openstreetmap.org/#map=19/51.19713999999999/3.217950000000002";
            // around 1km to the east
            var kathelijnevest = "https://www.openstreetmap.org/#map=19/51.2002/3.22909999999999";
            // around 1km to the west of the station
            var barrierestraat = "https://www.openstreetmap.org/#map=19/51.1944/3.20679999999999";

            // save them into the reader
            osmReader.MoveTo(stationBrugge);
            osmReader.MoveTo(kathelijnevest);
            osmReader.MoveTo(barrierestraat);
            var cacher = osmReader.UseCache();


            var aroundKathelijne = cacher.StopsAround(kathelijnevest, 1000).ToList();
            Assert.True(aroundKathelijne.Any());
            Assert.Equal(stationBruggeExpected, aroundKathelijne[0].GlobalId);
            Assert.Equal((uint) 1, cacher.CacheCount());
            var aroundbarriere = cacher.StopsAround(barrierestraat, 1000).ToList();
            Assert.True(aroundbarriere.Any());
            Assert.Equal(stationBruggeExpected, aroundbarriere[0].GlobalId);
            Assert.Equal((uint) 2, cacher.CacheCount());

            cacher.MakeComplete();
            Assert.Equal((uint) 3, cacher.CacheCount());

            var aroundStation = cacher.StopsAround(stationBrugge, 1000).Select(x => x.GlobalId).ToList();
            Assert.Equal(2, aroundStation.Count());
            Assert.Contains("https://www.openstreetmap.org/#map=19/51.200199999999995/3.2290999999999883", aroundStation);
            Assert.Contains("https://www.openstreetmap.org/#map=19/51.1944/3.206799999999987", aroundStation);

            Assert.Equal((uint) 3, cacher.CacheCount());
        }
    }
}