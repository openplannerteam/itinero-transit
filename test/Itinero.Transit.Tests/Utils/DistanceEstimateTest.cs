using Itinero.Transit.Utils;
using Xunit;

namespace Itinero.Transit.Tests.Utils
{
    public class DistanceEstimateTest
    {
        [Fact]
        public void TestTileNumbering()
        {
            var nw = DistanceEstimate.NorthWestCoordinateOfTile((0, 0), 1);
            var se = DistanceEstimate.NorthWestCoordinateOfTile((1, 1), 1);

            Assert.Equal(-180, nw.Item2);
            Assert.Equal(85, (int) nw.Item1);

            
        }
    }
}