using System;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
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

            // Prevent depletion of the DB
            connectionsDb.Add(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 20, 00, 00), 10 * 60, 0);

            
            var profile = new Profile<TransferStats>(
                connectionsDb, stopsDb, new InternalTransferGenerator(), new TransferStats(),
                TransferStats.ProfileTransferCompare);
            var eas = new EarliestConnectionScan<TransferStats>(
                stop1, stop2, new DateTime(2018, 12, 04, 16, 00, 00), new DateTime(2018, 12, 04, 19, 00, 00),
                profile);
            var journey = eas.CalculateJourney();

            Assert.NotNull(journey);
        }
    }
}