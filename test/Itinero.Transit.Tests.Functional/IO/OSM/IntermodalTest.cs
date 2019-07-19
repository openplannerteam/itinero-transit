using Itinero.Transit.Data;
using Itinero.Transit.IO.OSM;
using Itinero.Transit.IO.OSM.Data;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Tests.Functional.Algorithms;
using Itinero.Transit.Tests.Functional.Staging;

namespace Itinero.Transit.Tests.Functional.IO.OSM
{
    public class IntermodalTest : FunctionalTest<object, (string start, string destination)>
    {
        protected override object Execute((string start, string destination) input)
        {
            var tdb = TransitDb.ReadFrom(Constants.Nmbs, 0);
            
            var gen = new OsmTransferGenerator(RouterDbStaging.RouterDb, 2000,
                Profiles.Lua.Osm.OsmProfiles.Bicycle
            );
            
            var cached = gen.UseCache();
            
            // We create a router from the TDB and amend it with an OSM-Locations-Reader to decode OSM-coordinates
            var router0 = tdb.SelectProfile(new Profile<TransferMetric>(
                        new InternalTransferGenerator(),
                        cached, 
                        TransferMetric.Factory,
                        TransferMetric.ParetoCompare
                    ))
                    .UseOsmLocations()
                    .SelectStops(
                        input.start,
                        input.destination)
                    .SelectTimeFrame(Constants.TestDate, Constants.TestDate.AddHours(10));

            var earliestArrival = router0.EarliestArrivalJourney();
            NotNull(earliestArrival);
            router0.ResetFilter();
            var latestDeparture = router0.LatestDepartureJourney();
            NotNull(latestDeparture);
            router0.ResetFilter();
            True(router0.AllJourneys().Count > 0);

            return true;
        }
    }
}