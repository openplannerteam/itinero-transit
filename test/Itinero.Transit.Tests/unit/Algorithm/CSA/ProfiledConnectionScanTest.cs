using System;
using System.Linq;
using Itinero.IO.LC;
using Itinero.IO.LC.Tests;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Xunit;

namespace Itinero.Transit.Tests.unit.Algorithm.CSA
{
    public class ProfiledConnectionScanTest
    {
        [Fact]
        public void TestPcsSimple()
        {
            var db = Db.GetDefaultTestDb();

            var profile = new Profile<TransferStats>(
                db, Db.GetDefaultStopsDb(), new InternalTransferGenerator(60),
                TransferStats.Factory, TransferStats.ProfileTransferCompare);

            var pcs = new ProfiledConnectionScan<TransferStats>(
                (0, 0), (0, 3),
                new DateTime(2018, 12, 04, 16, 00, 00),
                new DateTime(2018, 12, 04, 18, 00, 00),
                profile);

            var journeys = pcs.CalculateJourneys();
            Assert.True(journeys.Count() > 0);
            
            
            
        }
    }
}