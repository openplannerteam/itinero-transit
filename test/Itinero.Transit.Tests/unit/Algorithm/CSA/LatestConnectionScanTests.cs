using System;
using System.Linq;
using Itinero.IO.LC.Tests;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Xunit;
using Xunit.Abstractions;

namespace Itinero.Transit.Tests.unit.Algorithm.CSA
{
    public class LatestConnectionScanTests : SuperTest
    {
        public LatestConnectionScanTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void SimpleLasTest()
        {
            var db = Db.GetDefaultTestDb();
            var stops = Db.GetDefaultStopsDb();

            var profile = new Profile<TransferStats>(
                db, stops, new InternalTransferGenerator(0), new TransferStats(),
                TransferStats.ProfileTransferCompare
            );

            var Las = new LatestConnectionScan<TransferStats>(
                (0, 0), (0, 1),
                new DateTime(2018, 12, 04, 16, 00, 00), new DateTime(2018, 12, 04, 18, 00, 00),
                profile
            );

            var j = Las.CalculateJourney();

            Assert.NotNull(j);
            Assert.Equal((uint) 0, j.Connection);


            Las = new LatestConnectionScan<TransferStats>(
                (0, 0), (0, 2), db.GetConn(0).DepartureTime, db.GetConn(0).DepartureTime + 60 * 60 * 2,
                profile
            );

            j = Las.CalculateJourney();

            Assert.NotNull(j);
            Assert.Equal((uint) 1, j.Connection);
        }

        [Fact]
        public void LatestConnectionScan_ShouldFindOneConnectionJourney()
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
            var Las = new LatestConnectionScan<TransferStats>(
                stop1, stop2, new DateTime(2018, 12, 04, 16, 00, 00), new DateTime(2018, 12, 04, 19, 00, 00),
                profile);
            var journey = Las.CalculateJourney();

            Assert.NotNull(journey);
            Assert.Equal(2, journey.AllParts().Count());
        }
    }
}