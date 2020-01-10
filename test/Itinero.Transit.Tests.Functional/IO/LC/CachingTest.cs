using System;
using Itinero.Transit.IO.LC.Data;
using Itinero.Transit.Tests.Functional.Utils;

namespace Itinero.Transit.Tests.Functional.IO.LC
{
    public class CachingTest : FunctionalTest
    {
        public override string Name => "Test Caching";

        protected override void Execute()
        {
            var cons = new ConnectionProvider(new Uri(Belgium.SncbConnections),
                Belgium.SncbConnections + "{?departureTime}");

            var (tt, _) =
                cons.GetTimeTable(
                    new Uri("https://graph.irail.be/sncb/connections"));

            var uri = tt.Uri;

            cons.GetTimeTable(
                uri);
            var (_, changed) =
                cons.GetTimeTable(
                    uri);
            True(!changed);
        }
    }
}