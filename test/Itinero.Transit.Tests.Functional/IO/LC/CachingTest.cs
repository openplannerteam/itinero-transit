using System;
using Itinero.Transit.IO.LC;
using Itinero.Transit.IO.LC.Data;

namespace Itinero.Transit.Tests.Functional.IO.LC
{
    public class CachingTest : FunctionalTest<bool, bool>
    {
        protected override bool Execute(bool input)
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
            return input;
        }
    }
}