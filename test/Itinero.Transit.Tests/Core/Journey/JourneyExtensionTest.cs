using Itinero.Transit.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Xunit;

namespace Itinero.Transit.Tests.Core.Journey
{
    public class JourneyExtensionTest
    {
        [Fact]
        public void TestReversed()
        {
            var j = new Journey<TransferMetric>(new LocationId(0, 0, 0), 0, TransferMetric.Factory);

            var j0 = j.Chain(new ConnectionId(0,1), 10, new LocationId(0, 0, 1), new TripId(0, 0));
            var j1 = j0.Chain(new ConnectionId(0,2), 20, new LocationId(0, 0, 2), new TripId(0, 0));


            var revs= j1.Reversed();
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
    }
}