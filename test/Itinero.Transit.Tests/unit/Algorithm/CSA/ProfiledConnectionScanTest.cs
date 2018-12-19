using System;
using System.Linq;
using Itinero.IO.LC;
using Itinero.IO.LC.Tests;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Xunit;
using Xunit.Abstractions;
// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests.unit.Algorithm.CSA
{
    public class ProfiledConnectionScanTest : SuperTest
    {
        public ProfiledConnectionScanTest(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TestPcsSimple()
        {
            var db = Db.GetDefaultTestDb();

            var profile = new Profile<TransferStats>(
                db, Db.GetDefaultStopsDb(), 
                new InternalTransferGenerator(60),
                new BirdsEyeInterwalkTransferGenerator(Db.GetDefaultStopsDb()), 
                TransferStats.Factory, TransferStats.ProfileTransferCompare);

            Pr("Starting PCS from (0,0) to (0,3)");

            var pcs = new ProfiledConnectionScan<TransferStats>(
                (0, 0), (0, 3),
                new DateTime(2018, 12, 04, 16, 00, 00),
                new DateTime(2018, 12, 04, 18, 00, 00),
                profile);

            var journeys = pcs.CalculateJourneys();


            Pr("---------------- DONE ----------------");
            foreach (var j in journeys)
            {
                Pr(j.ToString());
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
            var connDb = new ConnectionsDb();
            connDb.Add((0, 0), (0, 1),
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 00, 00),
                30 * 60, 0);


            connDb.Add((0, 0), (0, 1),
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 00, 00),
                40 * 60, 1);

            connDb.Add((1, 1), (2, 2), "https//example.com/connections/2",
                new DateTime(2018, 12, 04, 20, 00, 00),
                40 * 60, 2);
            
            connDb.Add((1, 1), (2, 2), "https//example.com/connections/4",
                new DateTime(2018, 12, 04, 2, 00, 00),
                40 * 60, 3);

            var profile = new Profile<TransferStats>(
                connDb, Db.GetDefaultStopsDb(), 
                new InternalTransferGenerator(60),
                new BirdsEyeInterwalkTransferGenerator(Db.GetDefaultStopsDb()), 
                TransferStats.Factory, TransferStats.ProfileTransferCompare);

            var pcs = new ProfiledConnectionScan<TransferStats>(
                (0, 0), (0, 1), new DateTime(2018, 12, 04, 16, 00, 00),
                new DateTime(2018, 12, 04, 18, 00, 00),
                profile);
            var journeys = pcs.CalculateJourneys();
            Assert.Single(journeys);
            foreach (var j in journeys)
            {
                Assert.Equal(30*60, (int) j.Stats.TravelTime);
            }
        }
    }
}