using System;
using Itinero.IO.Json;
using Itinero.Profiles.Lua.Osm;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.IO.OSM;
using Itinero.Transit.IO.OSM.Data;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Tests.Functional.Staging;

namespace Itinero.Transit.Tests.Functional.IO.OSM
{
    public class Itinero2RoutingTest : FunctionalTest<object, bool>
    {
        protected override object Execute(bool useOsmBetweenWalks)
        {
            // We create a transitDB with testdata from _nmbs.
            // Note that this database only contains connections of a certain date, namely TestAllAlgorithms.TestDate
            // Testing outside this range will give an error ("no connections loaded")
            var tdb = TransitDb.ReadFrom(Constants.Nmbs, 0);

            var stopsReader = tdb.Latest.StopsDb.GetReader()
                .UseCache()
                .AddOsmReader();

            var from = Constants.NearStationBruggeLatLonRijselse;
            var to = Constants.Brugge;

            stopsReader.MoveTo(from);
            var fromStop = new Stop(stopsReader);

            stopsReader.MoveTo(to);
            var toStop = new Stop(stopsReader);

            var gen = new OsmTransferGenerator(RouterDbStaging.RouterDb, 2000,
                OsmProfiles.Pedestrian
            );
            IOtherModeGenerator walks;
            if (!useOsmBetweenWalks)
            {
                walks = new FirstLastMilePolicy(
                    new CrowsFlightTransferGenerator(),
                    gen, fromStop,
                    gen, toStop
                );
            }
            else
            {
                var cached = gen.UseCache();
                Information("Precalculating the cache... Hang on...");
                var start = DateTime.Now;
                cached.PreCalculateCache(stopsReader);
                var end = DateTime.Now;
                Information($"Filling cache took {(end - start).TotalMilliseconds}ms");
                walks = cached;
            }


            // The profile of the traveller. This states that the traveller...
            var profile = new Profile<TransferMetric>( // Cares about both number of transfers and total travel time
                new InternalTransferGenerator(), // Needs 3 minutes to go from one train to another
                walks, // Likes walking far! The traveller is not afraid of walking over 2 kilometers between stops...
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


            var route = gen.CreateRoute(((float) fromStp.Latitude, (float) fromStp.Longitude),
                ((float) toStp.Latitude, (float) toStp.Longitude), out _, out _);
            
            NotNull(route, "Route not found");
            Console.WriteLine(route.ToGeoJson());
            True(route.TotalTime > 1, "The route is too short, walking to the station should take a few minutes");
            
            // This routing test only contains a walk: from somewhere in Bruges towards the station of Bruges
            // Buses are not loaded, so walking is the only option
            var easJ = router
                .SelectStops(from, to)
                .SelectTimeFrame(Constants.TestDate, Constants.TestDate.AddHours(10))
                .EarliestArrivalJourney();
            NotNull(easJ);
            Information(easJ.ToString(router));

            // can we get from bruges to ghent?
            easJ = router
                .SelectStops(Constants.Brugge, Constants.Gent)
                .SelectTimeFrame(Constants.TestDate, Constants.TestDate.AddHours(10))
                .EarliestArrivalJourney();
            NotNull(easJ);
            
            
            // And an attempt to reach Ghent from that same location!
            easJ = router
                .SelectStops(
                    from,
                    Constants.Gent)
                .SelectTimeFrame(Constants.TestDate, Constants.TestDate.AddHours(10))
                .EarliestArrivalJourney();
            NotNull(easJ);
            Information(easJ.ToString(router));
            True(easJ.Metric.WalkingTime > 1);


            return true;
        }
    }
}