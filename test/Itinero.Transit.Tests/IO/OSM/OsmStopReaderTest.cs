using Itinero.Transit.IO.OSM.Data;
using Xunit;

namespace Itinero.Transit.Tests.IO.OSM
{
    public class OsmStopReaderTest
    {
        [Fact]
        public void MoveTo_InvalidFormat_False()
        {
            var osmStopReader = new OsmLocationStopReader(0);
            var result = osmStopReader.MoveTo("https://totallyNotOsm.org/lat/qsdf");
            Assert.False(result);
        }
    }
}