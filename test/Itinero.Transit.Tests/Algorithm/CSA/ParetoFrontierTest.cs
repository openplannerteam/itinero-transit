using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Journeys;
using Itinero.Transit.Tests.Data;
using Xunit;

namespace Itinero.Transit.Tests.Algorithm.CSA
{
    public class ParetoFrontierTest
    {
        [Fact]
        public void SimpleFrontierTest()
        {
            var frontier = new ParetoFrontier<TransferMetric>(TransferMetric.ProfileTransferCompare);

            var loc = new LocationId(0, 0, 0);
            var loc1 = new LocationId(0, 0, 1);
            var loc2 = new LocationId(0, 0, 2);
            
            var j = new Journey<TransferMetric>(loc, 0, TransferMetric.Factory);

            j = j.ChainForward(new ConnectionMock(0, 0, 10, 0, loc, loc1));
            j = j.ChainForward(new ConnectionMock(1, 20, 30, 1, loc1, loc2));


            Assert.True(frontier.AddToFrontier(j));


            var direct = new Journey<TransferMetric>(loc, 40, TransferMetric.Factory);
            direct = direct.ChainBackward(new ConnectionMock(2, 0, 40, 2, loc, loc2));
            Assert.True(frontier.AddToFrontier(direct));


            var trSlow = new Journey<TransferMetric>(loc, 45, TransferMetric.Factory);

            trSlow = trSlow.ChainBackward(new ConnectionMock(1, 20, 45, 3, loc1, loc2));
            trSlow = trSlow.ChainBackward(new ConnectionMock(0, 0, 10, 0, loc, loc1));
            Assert.False(frontier.AddToFrontier(trSlow));
        }
    }
}