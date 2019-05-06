using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Journeys;
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

            j = j.ChainForward(new SimpleConnection(0, "00", loc, loc1, 0, 0, 10, 0, 0, new TripId(0, 0)));
            j = j.ChainForward(new SimpleConnection(0, "01", loc1, loc2, 1, 20, 30, 1, 0, new TripId(0, 1)));


            Assert.True(frontier.AddToFrontier(j));


            var direct = new Journey<TransferMetric>(loc, 40, TransferMetric.Factory);
            direct = direct.ChainBackward(new SimpleConnection(2, "02", loc, loc2, 2, 0, 40, 2, 0, new TripId(0, 2)));
            Assert.True(frontier.AddToFrontier(direct));


            var trSlow = new Journey<TransferMetric>(loc, 45, TransferMetric.Factory);

            trSlow = trSlow.ChainBackward(new SimpleConnection(0, "03", loc1, loc2, 1, 20, 45, 3, 0, new TripId(0, 2)));
            trSlow = trSlow.ChainBackward(new SimpleConnection(0, "04", loc1, loc2, 1, 20, 45, 3, 0, new TripId(0, 3)));
            Assert.False(frontier.AddToFrontier(trSlow));
        }
    }
}