using Itinero.IO.Osm.Tiles;
using Itinero.Profiles.Lua;
using Itinero.Profiles.Lua.Osm;
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
            TestRijselsestraatBrugge2Station();
            
            // We create a transitDB with testdata from _nmbs.
            // Note that this database only contains connections of a certain date, namely TestAllAlgorithms.TestDate
            // Testing outside this range will give an error ("no connections loaded")
            var tdb = TransitDb.ReadFrom(TestAllAlgorithms._nmbs, 0);


            var from = Constants.NearStationBruggeLatLonRijselse;
            var to = Constants.Brugge;


            OsmTransferGenerator.EnableCaching("./cache");
            var gen = new OsmTransferGenerator(
                5000,
                OsmProfiles.Pedestrian
            );


            // The profile of the traveller. This states that the traveller...
            var profile = new Profile<TransferMetric>( // Cares about both number of transfers and total travel time
                new InternalTransferGenerator(), // Needs 3 minutes to go from one train to another
                gen
                    .UseCache(), // Likes walking far! The traveller is not afraid of walking over 2 kilometers between stops...
                TransferMetric.Factory, // Actual boiler plate code
                TransferMetric
                    .ParetoCompare // This is the actual comparator which drives the selection of routes
            );

            Information("Performing a routing test. This might take some internet and time on first use");

            // We create a router from the TDB and amend it with an OSM-Locations-Reader to decode OSM-coordinates
            var router = tdb.SelectProfile(profile)
                    .AddStopsReader(new OsmLocationStopReader(1)) // This makes sure that osm.org-urls can be parsed
                ;
            var stops = router.StopsReader;
            stops.MoveTo(from);
            var fromStp = new Stop(stops);
            stops.MoveTo(to);
            var toStp = new Stop(stops);


            var route = gen.CreateRoute(fromStp, toStp, out _);
            NotNull(route, "Route not found");

            // This routing test only contains a walk: from somewhere in Bruges towards the station of Bruges
            // Buses are not loaded, so walking is the only option
            var easJ = router
                .SelectStops(from, to)
                .SelectTimeFrame(TestAllAlgorithms.TestDate, TestAllAlgorithms.TestDate.AddHours(10))
                .EarliestArrivalJourney();
            NotNull(easJ);
            Information(easJ.ToString(router));


            // And an attempt to reach Ghent from that same location!
            easJ = router.SelectStops("https://www.openstreetmap.org/#map=19/51.21460/3.21811",
                    Constants.Gent)
                .SelectTimeFrame(TestAllAlgorithms.TestDate, TestAllAlgorithms.TestDate.AddHours(10))
                .EarliestArrivalJourney();
            NotNull(easJ);
            True(easJ.Metric.WalkingTime > 600);
            Information(easJ.ToString(router));


            return true;
        }
        
        private void TestRijselsestraatBrugge2Station()
        {
            var routerDb = new RouterDb();
            routerDb.DataProvider = new DataProvider(routerDb);

            var pedestrian = OsmProfiles.Pedestrian;


            var p = new OsmTransferGenerator(5000, profile: pedestrian);
            // Rijselstraat, just behind the station
            var from = new Stop("a", new LocationId(0, 0, 0),
                3.2137800000000141, 51.193350000000009, null);
            var to = new Stop("b", new LocationId(0, 0, 1),
                3.2167249917984009, 51.197229555160746, null);

            var route = p.CreateRoute(from, to, out _);
            NotNull(route);
            True(route.Shape.Count > 10);
        }
    }
}