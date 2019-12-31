using Itinero.Transit.Utils;
using Xunit;

namespace Itinero.Transit.Tests.Utils
{
    public class DistanceEstimateTest
    {
        [Fact]
        public void NorthWestCoordinate_ZeroZoom_AngleBounds()
        {
            var nw = DistanceEstimate.NorthWestCoordinateOfTile((0, 0), 0);

            Assert.Equal(-180, nw.Item1);
            Assert.Equal(85, (int) nw.Item2);
            var se = DistanceEstimate.NorthWestCoordinateOfTile((1, 1), 0);

            Assert.Equal(180, se.Item1);
            Assert.Equal(-85, (int) se.Item2);

            
        }
        
        [Fact]
        public void NorthWestCoordinate_NextTile()
        {
            var nw = DistanceEstimate.NorthWestCoordinateOfTile((2400, 800), 14);
            var se = DistanceEstimate.NorthWestCoordinateOfTile((2401, 801 ), 14);

            Assert.True(nw.lat > se.lat); // y1 < y2 ==> lat1 > lat2
            Assert.True(nw.lon < se.lon); // x1 < x2 ==> lon1 < lon2

            
        }
    }
}