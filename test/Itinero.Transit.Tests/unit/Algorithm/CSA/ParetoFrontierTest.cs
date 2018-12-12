using Itinero.IO.LC;
using Itinero.Transit.Journeys;
using Itinero.Transit.Tests.Data;
using Xunit;

namespace Itinero.Transit.Tests.unit.Algorithm.CSA
{
    public class ParetoFrontierTest
    {
        [Fact]
        public void SimpleFrontierTest()
        {
            var frontier = new ParetoFrontier<TransferStats>(TransferStats.ProfileTransferCompare);

            var j = new Journey<TransferStats>((0, 0), 0, new TransferStats());

            j = j.ChainForward(new Connection(0, 0, 10, 0, (0, 0), (0, 1)));
            j = j.ChainForward(new Connection(1, 20, 30, 1, (0, 1), (0, 2)));


            Assert.True(frontier.AddToFrontier(j));


            var direct = new Journey<TransferStats>((0, 0), 40, new TransferStats());
            direct = direct.ChainBackward(new Connection(2, 0, 40, 2, (0, 0), (0, 2)));
            Assert.True(frontier.AddToFrontier(direct));


            var trSlow = new Journey<TransferStats>((0, 0), 45, new TransferStats());

            trSlow = trSlow.ChainBackward(new Connection(1, 20, 45, 3, (0, 1), (0, 2)));
            trSlow = trSlow.ChainBackward(new Connection(0, 0, 10, 0, (0, 0), (0, 1)));
            Assert.False(frontier.AddToFrontier(trSlow));
        }
    }
}