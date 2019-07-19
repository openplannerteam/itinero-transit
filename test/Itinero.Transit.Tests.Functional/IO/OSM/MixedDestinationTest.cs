using Itinero.Transit.Data;
using Itinero.Transit.IO.OSM.Data;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;

namespace Itinero.Transit.Tests.Functional.IO.OSM
{
    public class MixedDestinationTest : FunctionalTest<object, object>
    {
        protected override object Execute(object input)
        {
            var tdb = TransitDb.ReadFrom(Constants.Nmbs, 0);


            // We create a router from the TDB and amend it with an OSM-Locations-Reader to decode OSM-coordinates
            var router0 = tdb.SelectProfile(new Profile<TransferMetric>(
                        new InternalTransferGenerator(),
                        new CrowsFlightTransferGenerator(maxDistance: 2500), 
                        TransferMetric.Factory,
                        TransferMetric.ParetoCompare
                    ))
                    .UseOsmLocations()
                    .SelectStops(
                        "https://www.openstreetmap.org/#map=19/51.21460/3.21811",
                        Constants.Gent)
                    .SelectTimeFrame(Constants.TestDate, Constants.TestDate.AddHours(10))
                ;

            var earliestArrival = router0.EarliestArrivalJourney();
            NotNull(earliestArrival);
            router0.ResetFilter();
            var latestDeparture = router0.LatestDepartureJourney();
            NotNull(latestDeparture);
            router0.ResetFilter();
            True(router0.AllJourneys().Count > 0);


            var router1 = tdb.SelectProfile(new Profile<TransferMetric>(
                    new InternalTransferGenerator(),
                    new CrowsFlightTransferGenerator(2500),
                    TransferMetric.Factory,
                    TransferMetric.ParetoCompare
                ))
                .UseOsmLocations()
                .SelectStops(
                    Constants.Gent,
                    "https://www.openstreetmap.org/#map=19/51.21460/3.21811"
                )
                .SelectTimeFrame(Constants.TestDate, Constants.TestDate.AddHours(10));
            NotNull(router1.EarliestArrivalJourney());
            router1.ResetFilter();
            NotNull(router1.LatestDepartureJourney());
            router1.ResetFilter();
            True(router1.AllJourneys().Count > 0);


            return true;
        }
    }
}