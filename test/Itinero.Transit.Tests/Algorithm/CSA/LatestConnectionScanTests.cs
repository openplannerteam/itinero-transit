using System;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Xunit;

namespace Itinero.Transit.Tests.Algorithm.CSA
{
    public class LatestConnectionScanTests
    {
        [Fact]
        public void SimpleLasTest()
        {
            var tdb = Db.GetDefaultTestDb();
            var db = tdb.Latest;

            var profile = new Profile<TransferStats>(new InternalTransferGenerator(0),
                new CrowsFlightTransferGenerator(),
                TransferStats.Factory,
                TransferStats.ProfileTransferCompare
            );

            var stopsReader = tdb.Latest.StopsDb.GetReader();

            stopsReader.MoveTo("https://example.com/stops/0");
            var stop0 = stopsReader.Id;

            stopsReader.MoveTo("https://example.com/stops/1");
            var stop1 = stopsReader.Id;
            stopsReader.MoveTo("https://example.com/stops/2");
            var stop2 = stopsReader.Id;


            var las = new LatestConnectionScan<TransferStats>(new ScanSettings<TransferStats>(db,
                stop0, stop1,
                new DateTime(2018, 12, 04, 16, 00, 00),
                new DateTime(2018, 12, 04, 18, 00, 00),
                profile
            ));

            var j = las.CalculateJourney();

            Assert.NotNull(j);
            Assert.Equal((uint) 0, j.Connection);

            las = new LatestConnectionScan<TransferStats>(
                new ScanSettings<TransferStats>(
                    db,
                    stop0, stop2,
                    db.GetConn(0).DepartureTime.FromUnixTime(),
                    (db.GetConn(0).DepartureTime + 60 * 60 * 2).FromUnixTime(),
                    profile
                ));

            j = las.CalculateJourney();

            Assert.NotNull(j);
            Assert.Equal(j.Root.Location, stop0);
            Assert.Equal(j.Location, stop2);
        }


        [Fact]
        public void LatestConnectionScan_ShouldFindNoConnectionJourney()
        {
            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.1, 0.1);

            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00), 10 * 60, 0, 0, 0, 3); // MODE 3 - cant get on or off

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 20, 00, 00), 10 * 60, 0, 0, 0, 3);
            writer.Close();
            var latest = transitDb.Latest;

            var profile = new Profile<TransferStats>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferStats.Factory,
                TransferStats.ProfileTransferCompare);
            var las = new LatestConnectionScan<TransferStats>(
                new ScanSettings<TransferStats>(latest,
                    stop1, stop2,
                    new DateTime(2018, 12, 04, 16, 00, 00),
                    new DateTime(2018, 12, 04, 19, 00, 00),
                    profile));
            var journey = las.CalculateJourney();

            Assert.Null(journey);
        }

        [Fact]
        public void LatestConnectionScan_ShouldFindOneConnectionJourney()
        {
            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.1, 0.1);

            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00), 10 * 60, 0, 0, 0, 0);

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 20, 00, 00), 10 * 60, 0, 0, 0, 0);
            writer.Close();
            var latest = transitDb.Latest;

            var profile = new Profile<TransferStats>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferStats.Factory,
                TransferStats.ProfileTransferCompare);
            var las = new LatestConnectionScan<TransferStats>(
                new ScanSettings<TransferStats>(latest,
                    stop1, stop2,
                    new DateTime(2018, 12, 04, 16, 00, 00),
                    new DateTime(2018, 12, 04, 19, 00, 00),
                    profile));
            var journey = las.CalculateJourney();

            Assert.NotNull(journey);
            Assert.Equal(2, journey.AllParts().Count());
        }
    }
}