using System;
using System.Linq;
using Itinero.IO.LC;
using Itinero.IO.LC.Tests;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Xunit;
using Xunit.Abstractions;

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
                db, Db.GetDefaultStopsDb(), new InternalTransferGenerator(60),
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

       
    }
}