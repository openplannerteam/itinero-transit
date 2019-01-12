using Itinero.Transit.Data;
using Xunit;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class TripHeadsignTest : FunctionalTest<uint, TransitDb>
    {
        public static TripHeadsignTest Default = new TripHeadsignTest();
        
        protected override uint Execute(TransitDb input)
        {
            var latest = input.Latest;
            
            Information("Testing headsign attribute");
            var trip = latest.TripsDb.GetReader();
            var cons = latest.ConnectionsDb.GetDepartureEnumerator();
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