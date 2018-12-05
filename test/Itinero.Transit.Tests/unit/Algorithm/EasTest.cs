using System;
using Itinero.IO.LC.Tests;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Xunit;

namespace Itinero.Transit.Tests.Algorithm
{
    public class EasTest
    {


        [Fact]
        public void SimpleEASTest()
        {

            var db = Db.GetDefaultTestDb();
            var stops = Db.GetDefaultStopsDb();
            
            var profile = new Profile<TransferStats>(
                db, stops, new NoWalksGenerator(),new TransferStats()
                );
            
            var eas = new EarliestConnectionScan<TransferStats>(
                0, 1, db.GetConn(0).DepartureTime, db.GetConn(0).DepartureTime + 60*60*2,
                profile
                );

        }
        
    }
}