using Itinero.Transit.Algorithms.CSA;
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
            var frontier = new ParetoFrontier<TransferStats>(TransferStats.ProfileTransferCompare);

            var j = new Journey<TransferStats>((0, 0), 0, TransferStats.Factory);

            j = j.ChainForward(new ConnectionMock(0, 0, 10, 0, (0, 0), (0, 1)));
            j = j.ChainForward(new ConnectionMock(1, 20, 30, 1, (0, 1), (0, 2)));


            Assert.True(frontier.AddToFrontier(j));


            var direct = new Journey<TransferStats>((0, 0), 40, TransferStats.Factory);
            direct = direct.ChainBackward(new ConnectionMock(2, 0, 40, 2, (0, 0), (0, 2)));
            Assert.True(frontier.AddToFrontier(direct));


            var trSlow = new Journey<TransferStats>((0, 0), 45, TransferStats.Factory);

            trSlow = trSlow.ChainBackward(new ConnectionMock(1, 20, 45, 3, (0, 1), (0, 2)));
            trSlow = trSlow.ChainBackward(new ConnectionMock(0, 0, 10, 0, (0, 0), (0, 1)));
            Assert.False(frontier.AddToFrontier(trSlow));
        }
    }
}