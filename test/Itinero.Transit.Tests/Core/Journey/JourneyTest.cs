using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Tests.Dummies;
using Xunit;

namespace Itinero.Transit.Tests.Core.Journey
{
    public class JourneyTest
    {
        [Fact]
        public void Chain_WithConnection_ExpectsChainedJourney()
        {
            var j = new Journey<TransferMetric>(new StopId(0, 0), 0, TransferMetric.Factory);

            var j0 = j.Chain(new ConnectionId(0, 0), 10, new StopId(0, 1), new TripId(0, 0));

            Assert.False(j0.SpecialConnection);
            Assert.Equal(j, j0.PreviousLink);
            Assert.Equal((ulong) 10, j0.Time);
        }

        [Fact]
        public void Reverse_Journey_ExpectsReversedJourney()
        {
            var j = new Journey<TransferMetric>(new StopId(0, 0), 0, TransferMetric.Factory);

            var j0 = j.Chain(new ConnectionId(0, 1), 10, new StopId(0, 1), new TripId(0, 0));
            var j1 = j0.Chain(new ConnectionId(0, 2), 20, new StopId(0, 2), new TripId(0, 0));


            var revs = j1.Reversed();
            Assert.Single(revs);
            var rev = revs[0];
            var parts = rev.ToList();

            Assert.Equal(3, parts.Count);
            // The roots should have the same debug tags
            Assert.Equal(j.Connection, parts[0].Connection);
            // Whereas the connections have an of-by-one:
            Assert.Equal(j0.Connection, parts[2].Connection);
            Assert.Equal(j1.Connection, parts[1].Connection);


            Assert.Equal(j.Time, parts[2].Time);
            Assert.Equal(j0.Time, parts[1].Time);
            Assert.Equal(j1.Time, parts[0].Time);
        }

        [Fact]
        public void Reverse_JourneyWithTransfer_ExpectsReversedJourneyWithCorrectTimes()
        {
            var stop0 = new StopId(0, 0);
            var stop1 = new StopId(0, 1);
            var stop2 = new StopId(0, 2);

            var cid0 = new ConnectionId(0, 0);
            var cid1 = new ConnectionId(0, 1);

            var tripId0 = new TripId(0, 0);
            var tripId1 = new TripId(0, 1);


            var c0 = new Connection( "c0", stop1, stop2, 9000, 600, 0, tripId0);
            var c1 = new Connection( "c1", stop0, stop1, 8000, 600, 0, tripId1);

            var j = new Journey<TransferMetric>(stop0, 10000, TransferMetric.Factory);
            var j0 = j.ChainBackward(cid0, c0);
            Assert.Equal((ulong) 9000, j0.Time);
            var jtrans = j0.ChainBackwardWith(
                new DummyStopsDb(), new InternalTransferGenerator(), c1.ArrivalStop);
            var j1 = jtrans.ChainBackward(cid1, c1);
            Assert.Equal((ulong) 8000, j1.Time);


            var revs = j1.Reversed();
            Assert.NotNull(revs);
            Assert.Single(revs);

            var rev = revs[0];

            Assert.NotNull(rev);
            
            Assert.Equal(rev.Root,     rev.PreviousLink.PreviousLink.PreviousLink);
            Assert.Equal((ulong) 8000, rev.Root.Time); // Genesis
            Assert.Equal((ulong) 8820, rev.PreviousLink.PreviousLink.Time); // First connection arrival time + transfertime
            Assert.Equal((ulong) 9000, rev.PreviousLink.Time); // Transfer
            Assert.Equal((ulong) 9600, rev.Time); // Second connection

        }
    }

  
}