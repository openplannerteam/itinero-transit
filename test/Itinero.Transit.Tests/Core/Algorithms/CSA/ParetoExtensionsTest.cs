using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Xunit;

namespace Itinero.Transit.Tests.Core.Algorithms.CSA
{
    public class ParetoExtensionsTest
    {
        [Fact]
        public void ExtendFrontiersTest()
        {
            // We have to use backwards journeys here as we have to use the ProfiledTransferCompare
            var loc0 = new LocationId(0, 0, 0);
            var loc1 = new LocationId(0, 0, 1);
            var loc2 = new LocationId(0, 0, 2);
            var loc3 = new LocationId(0, 0, 3);

            // Arrival at time 100
            var genesis = new Journey<TransferMetric>(loc2, 100, TransferMetric.Factory);

            // Departs at Loc1 at 90
            var atLoc1 = genesis.ChainBackward(
                new SimpleConnection(0, "0", loc1, loc2, 90, 10, 0, 0, 0, new TripId(0, 0)));

            // Departs at Loc0 at 50, no transfers
            var direct = atLoc1.ChainBackward(
                new SimpleConnection(0, "0", loc0, loc1, 50, 10, 0, 0, 0, new TripId(0, 0)));
            // Departs at Loc0 at 60, one transfers
            var transfered = atLoc1.ChainBackward(
                new SimpleConnection(0, "0", loc0, loc1, 60, 10, 0, 0, 0, new TripId(0, 1)));

            var loc0Frontier = new ParetoFrontier<TransferMetric>(TransferMetric.ProfileTransferCompare, null);
            loc0Frontier.AddToFrontier(transfered);
            loc0Frontier.AddToFrontier(direct);
            
            var extended = loc0Frontier.ExtendFrontierBackwards(
                // Arrives at loc0 at 55 => direct can not be taken anymore, transfered can
                new DummyReader(), new SimpleConnection(6, "6", loc3, loc0, 45, 10, 0, 0, 0, new TripId(0, 1)),
                new InternalTransferGenerator());
            
            Assert.Equal(transfered, extended.Frontier[0].PreviousLink);
            Assert.Single(extended.Frontier);
            
            extended = loc0Frontier.ExtendFrontierBackwards(
                // Arrives at loc0 at 45 => both direct and transfered can be taken
                new DummyReader(), new SimpleConnection(6, "6", loc3, loc0, 35, 10, 0, 0, 0, new TripId(0, 1)),
                new InternalTransferGenerator(0));
            // We expect both routes to be in the frontier... They are, but in a merged way
            // ------------------------------------------- last connection - transferedJourney
            Assert.Equal(transfered, extended.Frontier[0].PreviousLink.PreviousLink);
            // -------------------------------------- lest connection -------- transferObject - directjourney
            Assert.Equal(direct, extended.Frontier[0].AlternativePreviousLink.PreviousLink.PreviousLink);
        }
        

        [Fact]
        public void CombineFrontiersTest()
        {
            var loc0 = new LocationId(0, 0, 0);
            var loc1 = new LocationId(0, 0, 1);
            var loc2 = new LocationId(0, 0, 2);


            // Genesis at time 0
            var genesis = new Journey<TransferMetric>(loc0, 0, TransferMetric.Factory);

            // Arrives at Loc1 at time 10
            var atLoc1 = genesis.ChainForward(
                new SimpleConnection(0, "0", loc0, loc1, 0, 10, 0, 0, 0, new TripId(0, 0)));

            // Arrives at Loc2 (destination)  at 25, without transfer but slightly slow 
            var direct = atLoc1.ChainForward(
                new SimpleConnection(1, "1", loc1, loc2, 15, 10, 0, 0, 0, new TripId(0, 0)));

            // Arrives at Loc2 (destination) slightly faster (at 21) but with one transfer
            var transferedFast = atLoc1.ChainForward(
                new SimpleConnection(2, "2", loc1, loc2, 11, 10, 0, 0, 0, new TripId(0, 1)));

            // Arrives at Loc2 (destination) slightly slower (at 23) and with one transfer
            var transferedSlow = atLoc1.ChainForward(
                new SimpleConnection(2, "2", loc1, loc2, 13, 10, 0, 0, 0, new TripId(0, 1)));


            // And now we add those to pareto frontier to test their behaviour
            var frontier0 = new ParetoFrontier<TransferMetric>(TransferMetric.ParetoCompare, null);

            Assert.True(frontier0.AddToFrontier(direct));
            Assert.True(frontier0.AddToFrontier(transferedFast));


            var frontier1 = new ParetoFrontier<TransferMetric>(TransferMetric.ParetoCompare, null);

            Assert.True(frontier1.AddToFrontier(direct));
            Assert.True(frontier1.AddToFrontier(transferedSlow));

            var frontier = ParetoExtensions.Combine(frontier0, frontier1);

            Assert.Equal(direct, frontier.Frontier[0]);
            Assert.Equal(transferedFast, frontier.Frontier[1]);
            Assert.DoesNotContain(transferedSlow, frontier.Frontier);
        }
    }
}