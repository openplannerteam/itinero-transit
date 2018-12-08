//using System;
//using System.Collections.Generic;
//using Itinero.IO.LC;
//using Reminiscence.Arrays;
//using Serilog;
//
//namespace Itinero.Transit.Tests.Functional.Tests
//{
//    public class TransitDbLoadingTest : FunctionalTest
//    {
//        public override void Test()
//        {
//            var testDb = GetTestDb(Brugge, Gent);
//            var connsDb = testDb.conns;
//            var ids = testDb.mapping;
//
//            var expected = testDb.count;
//            
//            Log.Information($"Source contains {testDb.count} direct trains from Bruges to Ghent");
//            
//            // Count how many connections between Brugge and Gent are available
//            var brugge = ids[Brugge];
//            var gent = ids[Gent];
//
//            var enumerator = connsDb.GetDepartureEnumerator();
//
//            var reverseMapping = new Dictionary<ulong, string>();
//            foreach (var pair in ids)
//            {
//                reverseMapping[pair.Value] = pair.Key;
//            }
//
//            Log.Information($"Brugge is {brugge}");
//            Log.Information($"Gent is {gent}");
//
//            var profile = Belgium.Sncb(new LocalStorage("cache"));
//
//            var found = 0;
//            while (enumerator.MoveNext())
//            {
//                if (enumerator.DepartureLocation == brugge
//                    && enumerator.ArrivalLocation == gent)
//                {
//                    found++;
//                }
//            }
//
//            Log.Information($"Found {found} entries");
//            if (found == 0)
//            {
//                throw new Exception(
//                    "Not a single connection between Bruges & Ghent... There might be something wrong. Try refreshing the cache, and check for railroad works on belgianrail.be. Or there might be a bug");
//            }
//
//            if (found != expected)
//            {
//                throw new Exception("The transit DB ate a connection between Bruges and Ghent");
//            }
//        }
//    }
//}