using System;
using Itinero.Transit.IO.LC;
using Itinero.Transit.IO.LC.Data;
using JsonLD.Core;

namespace Itinero.Transit.Tests.Functional.IO.LC
{
    public class CachingTest : FunctionalTest<bool, bool>
    {
        protected override bool Execute(bool input)
        {
            var cons = new ConnectionProvider(new Uri(Belgium.SNCB_Connections),
                Belgium.SNCB_Connections + "{?departureTime}");

            var (tt, _) =
                cons.GetTimeTable(
                    new Uri("https://graph.irail.be/sncb/connections"));


            var uri = tt.Uri;


            var (_, changed) =
                cons.GetTimeTable(
                    uri);
            (_, changed) =
                cons.GetTimeTable(
                    uri);
            True(!changed);
            return input;
        }
    }
}