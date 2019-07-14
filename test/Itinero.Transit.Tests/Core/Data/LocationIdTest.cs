using System.Collections.Generic;
using Itinero.Transit.Data.Core;
using Xunit;

namespace Itinero.Transit.Tests.Core.Data
{
    public class LocationIdTest
    {
        [Fact]
        public void TestLocationId()
        {
            var lid0 = new StopId(0, 0, 0);
            var lid1 = new StopId(0, 0, 1);
            var lid2 = new StopId(0, 0, 2);

            Assert.Equal(new StopId(0, 0, 0), lid0);
            Assert.Equal(new StopId(0, 0, 1).GetHashCode(), lid1.GetHashCode());


            var dict = new Dictionary<StopId, string>();

            dict.Add(lid2, "2");
            Assert.Equal("2", dict[new StopId(0, 0, 2)]);
        }
    }
}