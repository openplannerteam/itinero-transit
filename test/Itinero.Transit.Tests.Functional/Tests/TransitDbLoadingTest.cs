using System;
using System.Collections.Generic;
using Reminiscence.Arrays;
using Serilog;

namespace Itinero.Transit.Tests.Functional.Tests
{
    public class TransitDbLoadingTest : FunctionalTest
    {
        public override void Test()
        {
            var connsDb = GetTestDb().conns;
            var ids = GetTestDb().mapping;
            var stops = GetTestDb().stops.GetReader();
            // Count how many connections between Brugge and Gent are available
            var brugge = ids[Brugge];
            var gent = ids[Gent];

            var enumerator = connsDb.GetDepartureEnumerator();

            var reverseMapping = new Dictionary<(uint localTileId, uint localId), string>();
            foreach (var pair in ids)
            {
                reverseMapping[pair.Value] = pair.Key;
            }


            var found = 0;
            while (enumerator.MoveNext())
            {
                try
                {
                    Log.Information(
                        $"{reverseMapping[enumerator.DepartureStop]} -> {reverseMapping[enumerator.ArrivalStop]}");
     /*               if (enumerator.DepartureLocation == brugge && enumerator.ArrivalLocation == gent)
                        Log.Information($"Found a connection Brugge -> Gent at {enumerator.DepartureTime}");
                        found++;
                    }*/
                }
                catch (Exception e)
                {
                    Log.Information(e.Message);
                }
            }

            Log.Information($"Found {found} entries");
        }
    }
}