using System;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Xunit;

// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests.Algorithm.CSA
{
    public class ProfiledConnectionScanTest
    {
        [Fact]
        public void TestPcsSimple()
        {
            var tdb = Db.GetDefaultTestDb();
            var db = tdb.Latest;

            var profile = new Profile<TransferStats>(new InternalTransferGenerator(60),
                new CrowsFlightTransferGenerator(),
                TransferStats.Factory, TransferStats.ProfileTransferCompare);
            var stopsReader = tdb.Latest.StopsDb.GetReader();

            stopsReader.MoveTo("https://example.com/stops/0");
            var stop0 = stopsReader.Id;
           
            stopsReader.MoveTo("https://example.com/stops/3");
            var stop3 = stopsReader.Id;


            
            var pcs = new ProfiledConnectionScan<TransferStats>(new ScanSettings<TransferStats>(
                db,
                stop0, stop3,
                new DateTime(2018, 12, 04, 16, 00, 00),
                new DateTime(2018, 12, 04, 18, 00, 00),
                profile));

            var journeys = pcs.CalculateJourneys();

            //Pr("---------------- DONE ----------------");
            foreach (var j in journeys)
            {
                //Pr(j.ToString());
                Assert.True(Equals(stop0, j.Root.Location));
                Assert.True(Equals(stop3, j.Location));
            }

            Assert.Equal(2, journeys.Count());
        }

        /// <summary>
        /// This test gives two possible routes to PCS:
        /// one which is clearly better then the other.
        /// </summary>
        [Fact]
        public static void TestFiltering()
        {
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            writer.AddOrUpdateStop("https://example.com/stops/1", 0.1, 0.1);

            writer.AddOrUpdateConnection((0, 0), (0, 1),
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 00, 00),
                30 * 60, 0,0, 0, 0);


            writer.AddOrUpdateConnection((0, 0), (0, 1),
                "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 16, 00, 00),
                40 * 60, 0,0, 1, 0);

            writer.AddOrUpdateConnection((1, 1), (2, 2), "https//example.com/connections/2",
                new DateTime(2018, 12, 04, 20, 00, 00),
                40 * 60, 0,0, 2, 0);

            writer.AddOrUpdateConnection((1, 1), (2, 2), "https//example.com/connections/4",
                new DateTime(2018, 12, 04, 2, 00, 00),
                40 * 60, 0,0, 3, 0);

            writer.Close();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferStats>(new InternalTransferGenerator(60),
                new CrowsFlightTransferGenerator(),
                TransferStats.Factory, TransferStats.ProfileTransferCompare);

            var pcs = new ProfiledConnectionScan<TransferStats>
            (new ScanSettings<TransferStats>(
                latest,
                (0, 0), (0, 1),
                new DateTime(2018, 12, 04, 16, 00, 00),
                new DateTime(2018, 12, 04, 18, 00, 00),
                profile));
            var journeys = pcs.CalculateJourneys();
            Assert.Single(journeys);
            foreach (var j in journeys)
            {
                Assert.Equal(30 * 60, (int) j.Stats.TravelTime);
            }
        }
        
        [Fact]
        public void ShouldFindNoConnectionJourney()
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
            var pcs = new ProfiledConnectionScan<TransferStats>(
                new ScanSettings<TransferStats>(latest,
                    stop1, stop2,
                    new DateTime(2018, 12, 04, 16, 00, 00),
                    new DateTime(2018, 12, 04, 19, 00, 00),
                    profile));
            var journey = pcs.CalculateJourneys();

            Assert.Null(journey);
        }
    }
}