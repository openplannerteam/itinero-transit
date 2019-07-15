using Itinero.Data.Attributes;
using Itinero.Transit.Data;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class TripHeadsignTest : FunctionalTest<uint, TransitDb>
    {
        protected override uint Execute(TransitDb input)
        {
            var latest = input.Latest;

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
            return failed;
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