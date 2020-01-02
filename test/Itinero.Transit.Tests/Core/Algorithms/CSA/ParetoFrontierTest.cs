using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Tests.Dummies;
using Xunit;

namespace Itinero.Transit.Tests.Core.Algorithms.CSA
{
    public class ParetoFrontierTest
    {
        [Fact]
        public void AddToFrontier_OneSlowOneFasterJourney_JourneysAreMerged()
        {
            // In some cases, journeys which perform equally good are merged as to save needed comparisons
            // A typical real-life example is a train stopping in A, B, C and a second train stopping in B, C, D. A traveller going from A to D has the choice to transfer in B and C
            // This check makes sure the merging is correct
            var locDep = new StopId(0, 0);
            var locA = new StopId(0, 1);
            var locB = new StopId(0, 2);
            var locDest = new StopId(0, 3);


            // Genesis at time 0
            var genesis = new Journey<TransferMetric>(locDep, 20, TransferMetric.Factory);

            
            var atLocA =
                genesis.ChainBackward(new ConnectionId(0,0),
                    new Connection("0", locDep, locA, 10, 10, 0, 0, 0, new TripId(0, 0)));
            var atLocB =
                genesis.ChainBackward(new ConnectionId(0,1), 
                    new Connection("1", locDep, locB, 10, 10, 0, 0, 0, new TripId(0, 1)));

            var commonConnection = new Connection(
                 "2", locA, locDest, 01, 9, 0, 0, 0,
                new TripId(0, 2));
            var atDestA =
                atLocA.ChainBackward(new ConnectionId(0, 2),commonConnection);
            var atDestB =
                atLocB.ChainBackward(new ConnectionId(0, 2),commonConnection);

            var frontier = new ProfiledParetoFrontier<TransferMetric>(TransferMetric.ParetoCompare, null);

            Assert.True(frontier.AddToFrontier(atDestA));
            Assert.True(frontier.AddToFrontier(atDestB));
            
            Assert.Single(frontier.Frontier);
            Assert.Equal(atDestA, frontier.Frontier[0].PreviousLink); // Should not really be regarded as 'previouslink', but rather as 'Left leg' and 'right leg' of a binary tree
            Assert.Equal(atDestB.PreviousLink, frontier.Frontier[0].AlternativePreviousLink);

        }

        [Fact]
        public void AddToFrontier_OneFastOneSlowJourney_SlowJourneyNotAdded()
        {
            var frontier = new ProfiledParetoFrontier<TransferMetric>(TransferMetric.ParetoCompare, null);

            var loc = new StopId(0, 0);
            var loc1 = new StopId(0, 1);
            var loc2 = new StopId(0, 2);

            // Backwards journey
            var j = new Journey<TransferMetric>(loc, 20, TransferMetric.Factory);
            j = j.ChainBackward(new ConnectionId(0,0),new Connection( "01", loc1, loc2, 20, 20, 30, 1, 0, new TripId(0, 1)));
            j = j.ChainBackward(new ConnectionId(0,0),new Connection( "00", loc, loc1, 10, 10, 10, 0, 0, new TripId(0, 0)));


            Assert.True(frontier.AddToFrontier(j));


            var direct = new Journey<TransferMetric>(loc, 40, TransferMetric.Factory);
            direct = direct.ChainBackward(new ConnectionId(0,2),new Connection( "02", loc, loc2, 2, 0, 40, 2, 0, new TripId(0, 2)));
            Assert.True(frontier.AddToFrontier(direct));


            var trSlow = new Journey<TransferMetric>(loc, 45, TransferMetric.Factory);

            trSlow = trSlow.ChainBackward(new ConnectionId(0,0),new Connection( "03", loc1, loc2, 1, 20, 45, 3, 0, new TripId(0, 2)));
            trSlow = trSlow.ChainBackward(new ConnectionId(0,0),new Connection( "04", loc1, loc2, 1, 20, 45, 3, 0, new TripId(0, 3)));
            Assert.False(frontier.AddToFrontier(trSlow));
        }
        
          [Fact]
        public void ExtendFrontierBackwards_Frontier_ExpectsExtendedFrontier()
        {
            // We have to use backwards journeys here as we have to use the ProfiledTransferCompare
            var loc0 = new StopId(0, 0);
            var loc1 = new StopId(0, 1);
            var loc2 = new StopId(0, 2);
            var loc3 = new StopId(0, 3);

            // Arrival at time 100
            var genesis = new Journey<TransferMetric>(loc2, 100, TransferMetric.Factory);

            // Departs at Loc1 at 90
            var atLoc1 = genesis.ChainBackward(new ConnectionId(0, 0),
                new Connection( "0", loc1, loc2, 90, 10, 0, 0, 0, new TripId(0, 0)));

            // Departs at Loc0 at 50, no transfers
            var direct = atLoc1.ChainBackward(new ConnectionId(0, 1),
                new Connection( "1", loc0, loc1, 50, 10, 0, 0, 0, new TripId(0, 0)));
            // Departs at Loc0 at 60, one transfers
            var transfered = atLoc1.ChainBackward(new ConnectionId(0, 2),
                new Connection( "2", loc0, loc1, 60, 10, 0, 0, 0, new TripId(0, 1)));

            var loc0Frontier = new ProfiledParetoFrontier<TransferMetric>(TransferMetric.ParetoCompare, null);
            loc0Frontier.AddToFrontier(transfered);
            loc0Frontier.AddToFrontier(direct);

            var extended = loc0Frontier.ExtendFrontierBackwards(
                // Arrives at loc0 at 55 => direct can not be taken anymore, transfered can
                new DummyStopsDb(),new ConnectionId(0, 6),
                new Connection( "6", loc3, loc0, 45, 10, 0, 0, 0, new TripId(0, 1)),
                new InternalTransferGenerator());

            Assert.Equal(transfered, extended.Frontier[0].PreviousLink);
            Assert.Single(extended.Frontier);

            extended = loc0Frontier.ExtendFrontierBackwards(
                // Arrives at loc0 at 45 => both direct and transfered can be taken
                new DummyStopsDb(),new ConnectionId(0, 6),
                new Connection( "6", loc3, loc0, 35, 10, 0, 0, 0,
                    new TripId(0, 1)),
                new InternalTransferGenerator(0));
            // We expect both routes to be in the frontier... They are, but in a merged way
            // ------------------------------------------- last connection - transferedJourney
            Assert.Equal(transfered, extended.Frontier[0]
                    .PreviousLink // A fake element - this is the merging journey
                    .PreviousLink // The actual journey
            );
            // -------------------------------------- lest connection -------- transferObject - directjourney
            Assert.Equal(direct, extended.Frontier[0].AlternativePreviousLink.PreviousLink);
        }

        [Fact]
        public void Combine_ThreeFrontiers_ExpectsOneFrontiers()
        {
            var loc0 = new StopId(0, 0);
            var loc1 = new StopId(0, 1);
            var loc2 = new StopId(0, 2);


            // Genesis at time 60
            var genesis = new Journey<TransferMetric>(loc0, 60, TransferMetric.Factory);

            // Departs from Loc1 at time 10s needed
            var atLoc1 = genesis.ChainBackward(new ConnectionId(0, 0),
                new Connection( "0", loc0, loc1, 50, 10, 0, 0, 0, new TripId(0, 0)));

            // Departs from Loc2 (destination) at 25s needed, without transfer but slightly slow 
            var direct = atLoc1.ChainBackward(new ConnectionId(0, 1),
                new Connection( "1", loc1, loc2, 35, 10, 0, 0, 0, new TripId(0, 0)));

            // Departs from Loc2 (destination) slightly faster (at 21s needed) but with one transfer
            var transferedFast = atLoc1.ChainBackward(new ConnectionId(0, 2),
                new Connection( "2", loc1, loc2, 39, 10, 0, 0, 0, new TripId(0, 1)));

            // Departs from Loc2 (destination) slightly slower (at 23s needed) and with one transfer - suboptimal
            var transferedSlow = atLoc1.ChainBackward(new ConnectionId(0, 3),
                new Connection( "3", loc1, loc2, 37, 10, 0, 0, 0, new TripId(0, 1)));


            // And now we add those to pareto frontier to test their behaviour
            var frontier0 = new ProfiledParetoFrontier<TransferMetric>(TransferMetric.ParetoCompare, null);

            Assert.True(frontier0.AddToFrontier(direct));
            Assert.True(frontier0.AddToFrontier(transferedFast));


            var frontier1 = new ProfiledParetoFrontier<TransferMetric>(TransferMetric.ParetoCompare, null);

            Assert.True(frontier1.AddToFrontier(direct));
            Assert.True(frontier1.AddToFrontier(transferedSlow));

            var frontier = ParetoExtensions.Combine(frontier0, frontier1);

            Assert.Equal(2, frontier.Frontier.Count);
            Assert.Equal(direct, frontier.Frontier[0]);
            Assert.Equal(transferedFast, frontier.Frontier[1]);
            Assert.DoesNotContain(transferedSlow, frontier.Frontier);
        }
    }
}