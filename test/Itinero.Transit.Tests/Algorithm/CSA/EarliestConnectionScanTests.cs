using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Xunit;

namespace Itinero.Transit.Tests.Algorithm.CSA
{
    public class EarliestConnectionScanTests
    {
        [Fact]
        public void SimpleEasTest()
        {
            var tdb = Db.GetDefaultTestDb();
            var db = tdb.Latest;

            var profile = new Profile<TransferStats>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(db),
                TransferStats.Factory,
                TransferStats.ProfileTransferCompare
            );

            var eas = new EarliestConnectionScan<TransferStats>(db,
                (0, 0), (0, 1), db.GetConn(0).DepartureTime, db.GetConn(0).DepartureTime + 60 * 60 * 6,
                profile
            );

            var j = eas.CalculateJourney();

            Assert.NotNull(j);
            Assert.Equal((uint) 0, j.Connection);


            eas = new EarliestConnectionScan<TransferStats>(db,
                (0, 0), (0, 2), db.GetConn(0).DepartureTime, db.GetConn(0).DepartureTime + 60 * 60 * 2,
                profile
            );

            j = eas.CalculateJourney();

            Assert.NotNull(j);
            Assert.Equal((uint) 1, j.Connection);
        }


        [Fact]
        public void EarliestConnectionScan_WithWalk()
        {
            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/2", 0.001, 0.001); // very walkable distance
            var stop3 = writer.AddOrUpdateStop("https://example.com/stops/3", 60.1, 60.1);

            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 10, 00, 00), 10 * 60, 0,0,0);
            writer.AddOrUpdateConnection(stop2, stop3, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 10, 30, 00), 10 * 60, 0,0,1);

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/2",
                new DateTime(2018, 12, 04, 20, 00, 00), 10 * 60, 0,0, 2);

            writer.Close();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferStats>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(latest),
                new TransferStats(),
                TransferStats.ProfileTransferCompare);
            var eas = new EarliestConnectionScan<TransferStats>(latest,
                stop0, stop3, new DateTime(2018, 12, 04, 10, 00, 00), new DateTime(2018, 12, 04, 11, 00, 00),
                profile);
            var journey = eas.CalculateJourney();

            Assert.NotNull(journey);
            Assert.Equal(Journey<TransferStats>.WALK, journey.PreviousLink.PreviousLink.Connection);
            Assert.True(journey.PreviousLink.PreviousLink.SpecialConnection);
        }

        [Fact]
        public void EarliestConnectionScan_ShouldFindOneConnectionJourney()
        {
            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.1, 0.1);

            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00), 10 * 60, 0,0,0);

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 20, 00, 00), 10 * 60, 0,0,0);

            writer.Close();

            var latest = transitDb.Latest;
            var profile = new Profile<TransferStats>(new InternalTransferGenerator(),
                null,
                new TransferStats(),
                TransferStats.ProfileTransferCompare);
            var eas = new EarliestConnectionScan<TransferStats>(latest,
                stop1, stop2, new DateTime(2018, 12, 04, 16, 00, 00), new DateTime(2018, 12, 04, 19, 00, 00),
                profile);
            var journey = eas.CalculateJourney();

            Assert.NotNull(journey);
            Assert.Equal(2, journey.AllParts().Count());
        }

        [Fact]
        public void EarliestConnectionScan_ShouldFindOneConnectionJourneyWithArrivalTravelTime()
        {
            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.1, 0.1);

            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00), 10 * 60, 0,0,0);

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 20, 00, 00), 10 * 60, 0,0,0);

            writer.Close();

            var sources = new List<(uint tileId, uint localId, ulong travelTime)> {(stop1.tileId, stop1.localId, 0)};
            var targets = new List<(uint tileId, uint localId, ulong travelTime)> {(stop2.tileId, stop2.localId, 100)};

            var latest = transitDb.Latest;
            var profile = new Profile<TransferStats>(new InternalTransferGenerator(),
                null,
                new TransferStats(),
                TransferStats.ProfileTransferCompare);
            var eas = new EarliestConnectionScan<TransferStats>(
                transitDb,
                sources, targets, 
                new DateTime(2018, 12, 04, 16, 00, 00).ToUnixTime(), 
                new DateTime(2018, 12, 04, 19, 00, 00).ToUnixTime(),
                profile);
            var journey = eas.CalculateJourney();

            Assert.NotNull(journey);
            Assert.Equal(2, journey.AllParts().Count());
        }

        [Fact]
        public void EarliestConnectionScan_ShouldFindOneConnectionJourneyWithDepartureTravelTime()
        {
            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.1, 0.1);

            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00), 10 * 60, 0,0,0);

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 20, 00, 00), 10 * 60, 0,0,0);

            writer.Close();

            var sources = new List<(uint tileId, uint localId, ulong travelTime)> {(stop1.tileId, stop1.localId, 1000)};
            var targets = new List<(uint tileId, uint localId, ulong travelTime)> {(stop2.tileId, stop2.localId, 0)};

            var latest = transitDb.Latest;
            var profile = new Profile<TransferStats>(new InternalTransferGenerator(),
                null,
                new TransferStats(),
                TransferStats.ProfileTransferCompare);
            var eas = new EarliestConnectionScan<TransferStats>(
                transitDb,
                sources, targets, 
                new DateTime(2018, 12, 04, 16, 00, 00).ToUnixTime(), 
                new DateTime(2018, 12, 04, 19, 00, 00).ToUnixTime(),
                profile);
            var journey = eas.CalculateJourney();

            Assert.NotNull(journey);
            Assert.Equal(2, journey.AllParts().Count());
        }
    }
}