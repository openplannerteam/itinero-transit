using Itinero.Transit.Data;
using Xunit;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class TripHeadsignTest : FunctionalTest<uint, (ConnectionsDb c, TripsDb t)>
    {
        public static TripHeadsignTest Default = new TripHeadsignTest();
        
        protected override uint Execute((ConnectionsDb c, TripsDb t) input)
        {
            Information("Testing headsign attribute");
            var trip = input.t.GetReader();
            var cons = input.c.GetDepartureEnumerator();
            uint failed = 0;
            uint found = 0;
            while (cons.MoveNext())
            {

                trip.MoveTo(cons.TripId);
                trip.Attributes.TryGetValue("headsign", out var hs);
                if (hs == null)
                {
                    failed++;
                }
                else
                {
                    found++;
                }
            }
            Information($"Did not find {failed} headsigns");
            Assert.True(failed == 0);
            Assert.True(found > 0);
            return failed;
        }
    }
}