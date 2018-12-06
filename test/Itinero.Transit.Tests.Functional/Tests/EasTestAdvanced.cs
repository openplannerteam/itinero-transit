using System;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Serilog;
using Xunit;

namespace Itinero.Transit.Tests.Functional.Tests
{
    public class EasTestAdvanced : FunctionalTest
    {
        public override void Test()
        {
            var connections = GetTestDb().conns;
            var stopsDb = GetTestDb().stops;
            var p = new Profile<TransferStats>(
                connections, stopsDb,
                new NoWalksGenerator(), new TransferStats());

            var depTime = DateTime.Now.Date.AddHours(8);

            var eas = new EarliestConnectionScan<TransferStats>(
                GetLocation(Poperinge), GetLocation(Vielsalm),
                depTime.ToUnixTime(), depTime.AddHours(12).ToUnixTime(), p);

            var journey = eas.CalculateJourney();

            Assert.NotNull(journey);
            Log.Information(journey.ToString());
            // This will fail very hard on days when there are disruptions
            Assert.True(journey.Stats.TravelTime < 10 * 60 * 60);
            Assert.True(journey.Stats.NumberOfTransfers <= 8);
        }
    }
}