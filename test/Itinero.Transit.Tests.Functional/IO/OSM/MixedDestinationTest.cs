using Itinero.Transit.Data;
using Itinero.Transit.IO.OSM.Data;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Tests.Functional.Algorithms;

namespace Itinero.Transit.Tests.Functional.IO.OSM
{
    public class MixedDestinationTest : FunctionalTest<object, object>
    {
        protected override object Execute(object input)
        {
            var tdb = TransitDb.ReadFrom(TestAllAlgorithms._nmbs, 0);


            // We create a router from the TDB and amend it with an OSM-Locations-Reader to decode OSM-coordinates
            var router0 = tdb.SelectProfile(new Profile<TransferMetric>(
                        new InternalTransferGenerator(),
                        new CrowsFlightTransferGenerator(2500),
                        TransferMetric.Factory,
                        TransferMetric.ProfileTransferCompare
                    ))
                    .UseOsmLocations()
                    .SelectStops(
                        "https://www.openstreetmap.org/#map=19/51.21460/3.21811",
                        TestAllAlgorithms.Gent)
                    .SelectTimeFrame(TestAllAlgorithms.TestDate, TestAllAlgorithms.TestDate.AddHours(10))
                ;

            NotNull(router0.EarliestArrivalJourney());
            router0.ResetFilter();
            NotNull(router0.LatestDepartureJourney());
            router0.ResetFilter();
            True(router0.AllJourneys().Count > 0);


            var router1 = tdb.SelectProfile(new Profile<TransferMetric>(
                    new InternalTransferGenerator(),
                    new CrowsFlightTransferGenerator(2500),
                    TransferMetric.Factory,
                    TransferMetric.ProfileTransferCompare
                ))
                .UseOsmLocations()
                .SelectStops(
                    TestAllAlgorithms.Gent,
                    "https://www.openstreetmap.org/#map=19/51.21460/3.21811"
                )
                .SelectTimeFrame(TestAllAlgorithms.TestDate, TestAllAlgorithms.TestDate.AddHours(10));
            NotNull(router1.EarliestArrivalJourney());
            router1.ResetFilter();
            NotNull(router1.LatestDepartureJourney());
            router1.ResetFilter();
            True(router1.AllJourneys().Count > 0);


            return true;
        }
    }
}