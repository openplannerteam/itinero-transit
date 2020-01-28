using Itinero.Transit.Data;
using Itinero.Transit.Tests.Functional.Utils;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class TripHeadsignTest : FunctionalTestWithInput<TransitDb>
    {
        public override string Name => "Trip Headsign test";

        
        protected override void Execute()
        {
            var latest = Input.Latest;

            Information("Testing headsign attribute");
            var tripDb = latest.Trips;
            var connections = latest.Connections;
            uint failed = 0;
            uint found = 0;
            uint total = 0;

            foreach (var connection in connections)
            {
                total++;
                var trip = tripDb.Get(connection.TripId);
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
}