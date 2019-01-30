using System;
using System.Linq;
using Itinero.IO.LC;
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
            var db = Db.GetDefaultTestDb().Latest;

            var profile = new Profile<TransferStats>(
                db,
                new InternalTransferGenerator(60),
                new BirdsEyeInterWalkTransferGenerator(db.StopsDb.GetReader()),
                TransferStats.Factory, TransferStats.ProfileTransferCompare);

            //Pr("Starting PCS from (0,0) to (0,3)");

            var pcs = new ProfiledConnectionScan<TransferStats>(
                (0, 0), (0, 3),
                new DateTime(2018, 12, 04, 16, 00, 00),
                new DateTime(2018, 12, 04, 18, 00, 00),
                profile);

            var journeys = pcs.CalculateJourneys();


            //Pr("---------------- DONE ----------------");
            foreach (var j in journeys)
            {
                //Pr(j.ToString());
                Assert.True(Equals(((uint) 0, (uint) 0), j.Root.Location));
                Assert.True(Equals(((uint) 0, (uint) 3), j.Location));
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
            writer.AddOrUpdateStop("https://example.com/stops/0", 0.1, 0.1);

            writer.AddOrUpdateConnection((0, 0), (0, 1),
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 00, 00),
                30 * 60, 0);


            writer.AddOrUpdateConnection((0, 0), (0, 1),
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 00, 00),
                40 * 60, 1);

            writer.AddOrUpdateConnection((1, 1), (2, 2), "https//example.com/connections/2",
                new DateTime(2018, 12, 04, 20, 00, 00),
                40 * 60, 2);

            writer.AddOrUpdateConnection((1, 1), (2, 2), "https//example.com/connections/4",
                new DateTime(2018, 12, 04, 2, 00, 00),
                40 * 60, 3);

            writer.Close();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferStats>(
                latest,
                new InternalTransferGenerator(60),
                new BirdsEyeInterWalkTransferGenerator(latest.StopsDb.GetReader()),
                TransferStats.Factory, TransferStats.ProfileTransferCompare);

            var pcs = new ProfiledConnectionScan<TransferStats>(
                (0, 0), (0, 1), new DateTime(2018, 12, 04, 16, 00, 00),
                new DateTime(2018, 12, 04, 18, 00, 00),
                profile);
            var journeys = pcs.CalculateJourneys();
            Assert.Single(journeys);
            foreach (var j in journeys)
            {
                Assert.Equal(30 * 60, (int) j.Stats.TravelTime);
            }
        }
    }
}