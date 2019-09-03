using Itinero.Data.Attributes;
using Itinero.Transit.Data;
using Itinero.Transit.Tests.Functional.Utils;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class TripHeadsignTest : FunctionalTestWithInput<TransitDb>
    {
        protected override void Execute()
        {
            var latest = Input.Latest;

            Information("Testing headsign attribute");
            var tripDb = latest.TripsDb;
            var consReader = latest.ConnectionsDb;
            uint failed = 0;
            uint found = 0;
            uint total = 0;
            var index = consReader.First().Value;
            while (consReader.HasNext(index, out index))
            {
                total++;
                var trip = tripDb.Get(consReader.Get(index).TripId);
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
        }
    }

    internal static class Helpers
    {
        public static string Get(this IAttributeCollection attributes, string name)
        {
            attributes.TryGetValue(name, out var result);
            return result;
        }
    }
}