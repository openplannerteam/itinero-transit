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

            var (_, changed) =
                cons.GetTimeTable(
                    new Uri("https://graph.irail.be/sncb/connections?departureTime=2019-03-12T15:49:00.000Z"));

            True(changed);


            (_, changed) =
                cons.GetTimeTable(
                    new Uri("https://graph.irail.be/sncb/connections?departureTime=2019-03-12T15:49:00.000Z"));

            True(!changed);
            return input;
        }
    }
}