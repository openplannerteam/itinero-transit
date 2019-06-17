using Itinero.Transit.Data;
using Itinero.Transit.IO.OSM;
using Itinero.Transit.IO.OSM.Data;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Tests.Functional.Algorithms;

namespace Itinero.Transit.Tests.Functional.IO.OSM
{
    public class Itinero2RoutingTest : FunctionalTest<object, object>
    {
        protected override object Execute(object input)
        {
            // We create a transitDB with testdata from _nmbs.
            // Note that this database only contains connections of a certain date, namely TestAllAlgorithms.TestDate
            // Testing outside this range will give an error ("no connections loaded")
            var tdb = TransitDb.ReadFrom(TestAllAlgorithms._nmbs, 0);

            // The profile of the traveller. This states that the traveller...
            var profile = new Profile<TransferMetric>( // Cares about both number of transfers and total travel time
                new InternalTransferGenerator(180), // Needs 3 minutes to go from one train to another
                new OsmTransferGenerator(
                    searchDistance: 2500), // Likes walking far! The traveller is not afraid of walking over 2 kilometers between stops...
                TransferMetric.Factory, // Actual boiler plate code
                TransferMetric
                    .ProfileTransferCompare // This is the actual comparator which drives the selection of routes
            );

            Information("Performing a routing test. This might take some internet and time on first use");

            // We create a router from the TDB and amend it with an OSM-Locations-Reader to decode OSM-coordinates
            var router = tdb.SelectProfile(profile)
                    .AddStopsReader(new OsmLocationStopReader(1)) // This makes sure that osm.org-urls can be parsed
                ;
            
            // This routing test only contains a walk: from somewhere in Bruges towards the station of Bruges
            // Buses are not loaded, so walking is the only option
            var easJ = router
                .SelectStops("https://www.openstreetmap.org/#map=19/51.21460/3.21811",
                    TestAllAlgorithms.Brugge)
                
                .SelectTimeFrame(TestAllAlgorithms.TestDate, TestAllAlgorithms.TestDate.AddHours(10))
                .EarliestArrivalJourney();
            NotNull(easJ);
            Information(easJ.ToString(router));


            // And an attempt to reach Ghent from that same location!
            easJ = router.SelectStops("https://www.openstreetmap.org/#map=19/51.21460/3.21811",
                    TestAllAlgorithms.Gent)
                .SelectTimeFrame(TestAllAlgorithms.TestDate, TestAllAlgorithms.TestDate.AddHours(10))
                .EarliestArrivalJourney();
            NotNull(easJ);
            True(easJ.Metric.WalkingTime > 600);
            Information(easJ.ToString(router));


            return true;
        }
    }
}