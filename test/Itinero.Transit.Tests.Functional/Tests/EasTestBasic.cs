using System;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Serilog;
using Xunit;

namespace Itinero.Transit.Tests.Functional.Tests
{
    public class EasTestBasic : FunctionalTest
    {
        public override void Test()
        {
            var connections = GetTestDb().conns;
            var stopsDb = GetTestDb().stops;
            var p = new Profile<TransferStats>(
                connections, stopsDb,
                new NoWalksGenerator(), new TransferStats());

            var depTime = DateTime.Now.Date.AddMinutes(10 * 60 + 25);

            var eas = new EarliestConnectionScan<TransferStats>(
                GetLocation(Brugge), GetLocation(Gent),
                depTime.ToUnixTime(), depTime.AddHours(3).ToUnixTime(), p);

            GetLocation(Brugge);
            var journey = eas.CalculateJourney();

            Assert.NotNull(journey);
            Log.Information(journey.ToString());
            // This will fail very hard on days when there are works between Ghent & Bruges
            Assert.True(journey.Stats.TravelTime < 45 * 60);
            Assert.True(journey.Stats.NumberOfTransfers <= 1);
        }
    }
}