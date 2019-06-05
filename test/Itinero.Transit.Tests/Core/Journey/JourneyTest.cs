using Itinero.Transit.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Xunit;

namespace Itinero.Transit.Tests.Core.Journey
{
    public class JourneyTest
    {
        [Fact]
        public void TestChain()
        {
            var j = new Journey<TransferMetric>(new LocationId(0, 0, 0), 0, TransferMetric.Factory);

            var j0 = j.Chain(0, 10, new LocationId(0, 0, 1), new TripId(0, 0));

            Assert.False(j0.SpecialConnection);
            Assert.Equal(j, j0.PreviousLink);
            Assert.Equal((ulong) 10, j0.Time);
        }

        [Fact]
        public void TestTransfer()
        {
            var j = new Journey<TransferMetric>(new LocationId(0, 0, 0), 0, TransferMetric.Factory);

            var j0 = j.Transfer(10);

            Assert.True(j0.SpecialConnection);
            Assert.Equal(j, j0.PreviousLink);
            Assert.Equal(Journey<TransferMetric>.OTHERMODE, j0.Connection);
            Assert.Equal((ulong) 10, j0.Time);
        }
    }
}