using Itinero.Transit.Data;

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
            uint total = 0;
            while (cons.MoveNext())
            {
                total++;
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

            Information($"Headsign test: failed: {failed}, found {found}, total {total}");
            True(failed == 0);
            True(found > 0);
            return failed;
        }
    }
}