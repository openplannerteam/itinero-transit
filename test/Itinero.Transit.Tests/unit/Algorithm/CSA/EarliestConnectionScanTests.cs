using System;
using Itinero.IO.LC.Tests;
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
        public void SimpleEasTest()
        {
            var db = Db.GetDefaultTestDb();
            var stops = Db.GetDefaultStopsDb();

            var profile = new Profile<TransferStats>(
                db, stops, new InternalTransferGenerator(), new TransferStats(),
                TransferStats.ProfileTransferCompare
            );

            var eas = new EarliestConnectionScan<TransferStats>(
                (0, 0), (0, 1), db.GetConn(0).DepartureTime, db.GetConn(0).DepartureTime + 60 * 60 * 6,
                profile
            );

            var j = eas.CalculateJourney();

            Assert.NotNull(j);
            Assert.Equal((uint) 0, j.Connection);


            eas = new EarliestConnectionScan<TransferStats>(
                (0,0),(0, 2), db.GetConn(0).DepartureTime, db.GetConn(0).DepartureTime + 60 * 60 * 2,
                profile
            );

            j = eas.CalculateJourney();

            Assert.NotNull(j);
            Assert.Equal((uint) 1, j.Connection);
        }
        
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