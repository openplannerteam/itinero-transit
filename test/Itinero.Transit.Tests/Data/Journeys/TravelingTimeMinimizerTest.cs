using System;
using Itinero.Transit.Data;
using Itinero.Transit.Journeys;
using Xunit;

namespace Itinero.Transit.Tests.Data
{
    public class TravelingTimeMinimizerTest
    {
        [Fact]
        public void Ttm0()
        {
            var loc0 = new LocationId(0, 0, 0);
            var loc1 = new LocationId(0, 0, 1);
            var genesis =
                new Journey<TravellingTimeMinimizer>(loc0, new DateTime(2019, 03, 05, 10, 00, 00).ToUnixTime(),
                    TravellingTimeMinimizer.Factory);

            var j0 = genesis.ChainForward(new ConnectionMock(1,
                new DateTime(2019, 03, 05, 10, 05, 00).ToUnixTime(),
                new DateTime(2019, 03, 05, 10, 55, 00).ToUnixTime(),
                2,
                loc0, loc1
            ));

            var j1 = genesis.ChainForward(new ConnectionMock(1,
                new DateTime(2019, 03, 05, 10, 05, 00).ToUnixTime(),
                new DateTime(2019, 03, 05, 10, 50, 00).ToUnixTime(),
                2,
                loc0, loc1
            ));


            var comp = TravellingTimeMinimizer.Minimize;

            Assert.Equal(1, comp.ADominatesB(j0, j1));
            Assert.Equal(-1, comp.ADominatesB(j1, j0));

            Assert.Equal(0, comp.ADominatesB(j0, j0));
            Assert.Equal(0, comp.ADominatesB(j1, j1));
        }
    }
}