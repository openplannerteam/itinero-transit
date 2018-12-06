using System;
using System.Security.Cryptography;
using Itinero.IO.LC.Tests;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Xunit;

namespace Itinero.Transit.Tests.Algorithm
{
    public class EasTest
    {
        [Fact]
        public void SimpleEasTest()
        {
            var db = Db.GetDefaultTestDb();
            var stops = Db.GetDefaultStopsDb();

            var profile = new Profile<TransferStats>(
                db, stops, new NoWalksGenerator(), new TransferStats()
            );

            var eas = new EarliestConnectionScan<TransferStats>(
                0, 1, 
                db.GetConn(0).DepartureTime, db.GetConn(0).DepartureTime + 60 * 60 * 6,
                profile
            );

            var j = eas.CalculateJourney();

            Assert.NotNull(j);
            Assert.Equal((uint) 0, j.Connection);


            eas = new EarliestConnectionScan<TransferStats>(
                0, 2, db.GetConn(0).DepartureTime, db.GetConn(0).DepartureTime + 60 * 60 * 2,
                profile
            );

            j = eas.CalculateJourney();

            Assert.NotNull(j);
            Assert.Equal((uint) 1, j.Connection);
        }
    }
}