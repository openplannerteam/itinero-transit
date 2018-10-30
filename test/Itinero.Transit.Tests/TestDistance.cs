using Itinero.Transit.CSA;
using Xunit;
using Xunit.Abstractions;

namespace Itinero.Transit_Tests
{
    public class TestDistance
    {
        private readonly ITestOutputHelper _output;

        public TestDistance(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestStorage()
        {
            var lat = 51.21576f;
            var lon = 3.22048f;
            var lat0 = 51.21570f;
            var lon0 = 3.22048f;
            var dist = DistanceBetweenPoints.DistanceInMeters(lat, lon, lat0, lon0);
            Log("" + dist);
            Assert.Equal(6, (int) dist);
            var nautical = DistanceBetweenPoints.DistanceInMeters(0, 0, 1f/60, 0);
            Log("" + nautical);
            Assert.Equal(1852, (int) nautical);
            
        }

        // ReSharper disable once UnusedMember.Local
        private void Log(string s)
        {
            _output.WriteLine(s);
        }
    }
}