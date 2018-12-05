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

            var reverseMapping = new Dictionary<ulong, string>();
            foreach (var pair in ids)
            {
                reverseMapping[pair.Value] = pair.Key;
            }
            
            
            
            var found = 0;
            while (enumerator.MoveNext())
            {
                Log.Information($"{reverseMapping[enumerator.DepartureLocation]} -> {reverseMapping[enumerator.ArrivalLocation]}");
                if (enumerator.DepartureLocation == brugge && enumerator.ArrivalLocation == gent)
                {
                    Log.Information($"Found a connection Brugge -> Gent at {enumerator.DepartureTime}");
                    found++;
                }
            }
            
            Log.Information($"Found {found} entries");
            
        }
    }
}