using System;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Xunit;

namespace Itinero.Transit.Tests.unit.Algorithm.CSA
{
    public class EarliestConnectionScanTests
    {
        [Fact]
        public void EarliestConnectionScan_ShouldFindOneConnectionJourney()
        {
            // build a one-connection db.
            var stopsDb = new StopsDb();
            var stop1 = stopsDb.Add("https://example.com/stops/0", 0, 0.0);
            var stop2 = stopsDb.Add("https://example.com/stops/0", 0.1, 0.1);
            
            var connectionsDb = new ConnectionsDb();
            connectionsDb.Add(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00), 10 * 60, 0);

            var stops1Id = ((ulong)stop1.localTileId * uint.MaxValue) + stop1.localId;
            var stops2Id = ((ulong)stop2.localTileId * uint.MaxValue) + stop2.localId;

            var profile = new Profile<TransferStats>(
                connectionsDb, stopsDb, new NoWalksGenerator(), new TransferStats());
            var eas = new EarliestConnectionScan<TransferStats>(
                stops1Id, stops2Id, new DateTime(2018, 12, 04, 16, 00, 00), new DateTime(2018, 12, 04, 19, 00, 00),
                profile);
            var journey = eas.CalculateJourney();
            
            Assert.NotNull(journey);
        }
    }
}