using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Xunit;

namespace Itinero.Transit.Tests.Core.Algorithms.CSA
{
    public class ParetoFrontierTest
    {
        [Fact]
        public void MergeJourneysTest()
        {
            // In some cases, journeys which perform equally good are merged as to save needed comparisons
            // A typical real-life example is a train stopping in A, B, C and a second train stopping in B, C, D. A traveller going from A to D has the choice to transfer in B and C
            // This check makes sure the merging is correct
            var locDep = new LocationId(0, 0, 0);
            var locA = new LocationId(0, 0, 1);
            var locB = new LocationId(0, 0, 2);
            var locDest = new LocationId(0, 0, 3);


            // Genesis at time 0
            var genesis = new Journey<TransferMetric>(locDep, 0, TransferMetric.Factory);

            // TRIP 0

            var atLocA =
                genesis.ChainForward(new SimpleConnection(0, "0", locDep, locA, 0, 10, 0, 0, 0, new TripId(0, 0)));
            var atLocB =
                genesis.ChainForward(new SimpleConnection(1, "1", locDep, locB, 0, 10, 0, 0, 0, new TripId(0, 1)));

            var atDestA =
                atLocA.ChainForward(new SimpleConnection(2, "2", locA, locDest, 10, 10, 0, 0, 0, new TripId(0, 2)));
            var atDestB =
                atLocB.ChainForward(new SimpleConnection(3, "3", locA, locDest, 10, 10, 0, 0, 0, new TripId(0, 3)));

            var frontier = new ParetoFrontier<TransferMetric>(TransferMetric.ProfileTransferCompare, null);

            Assert.True(frontier.AddToFrontier(atDestA));
            Assert.True(frontier.AddToFrontier(atDestB));
            
            Assert.Single(frontier.Frontier);
            Assert.Equal(atDestA, frontier.Frontier[0].PreviousLink); // Should not really be regarded as 'previouslink', but rather as 'Left leg' and 'right leg' of a binary tree
            Assert.Equal(atDestB, frontier.Frontier[0].AlternativePreviousLink);

        }

        [Fact]
        public void SimpleFrontierTest()
        {
            var frontier = new ParetoFrontier<TransferMetric>(TransferMetric.ProfileTransferCompare, null);

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