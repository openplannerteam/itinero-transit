
using System.Collections.Generic;
using Itinero.Transit.Data;
using Xunit;

namespace Itinero.Transit.Tests.Algorithm.Data
{
    public class LocationIdTest
    {
        [Fact]
        public void TestLocationId()
        {
            var lid0 = new LocationId(0, 0, 0);
            var lid1 = new LocationId(0, 0, 1);
            var lid2 = new LocationId(0, 0, 2);

            Assert.Equal(new LocationId(0, 0, 0), lid0);
            Assert.Equal(new LocationId(0, 0, 1).GetHashCode(), lid1.GetHashCode());


            var dict = new Dictionary<LocationId, string>();

            dict.Add(lid2, "2");
            Assert.Equal("2", dict[new LocationId(0, 0, 2)]);
        }
    }
}