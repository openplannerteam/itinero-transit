using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Xunit;

namespace Itinero.Transit.Tests.Core.Algorithms.CSA
{
    public class EarliestConnectionScanTests
    {
        [Fact]
        public void EarliestArrivalJourney_SingleConnectionDb_RouteWithOneConnection()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0", (0, 0.0)));
            var stop2 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1", (0.1, 0.1)));

            writer.AddOrUpdateConnection(new Connection(
                stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0), 0));

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(new Connection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 20, 00, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 1), 0));

            transitDb.CloseWriter();

            var latest = transitDb.Latest;
            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                null,
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);


            var journey = latest.SelectProfile(profile)
                .SelectStops(stop1, stop2)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 19, 00, 00, DateTimeKind.Utc))
                .CalculateEarliestArrivalJourney();

            Assert.NotNull(journey);
            Assert.Equal(2, journey.AllParts().Count());
        }

        [Fact]
        public void EarliestArrivalJourney_SmallDb_OneConnectionJourney()
        {
            var tdb = Db.GetDefaultTestDb(out var stop0, out var stop1, out _, out _, out _, out _);
            var db = tdb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                null,
                TransferMetric.Factory,
                TransferMetric.ParetoCompare
            );


            var j = db
                .SelectProfile(profile)
                .SelectStops(stop0, stop1)
                .SelectTimeFrame(db.Connections.EarliestDate - 1,
                    db.Connections.LatestDate + 1)
                .CalculateEarliestArrivalJourney();

            Assert.NotNull(j);
            Assert.Equal("https://example.com/connections/0", db.Connections.Get(j.Connection).GlobalId);
        }


        [Fact]
        public void EarliestArrivalJourney_SmallDb_TwoConnectionJourney()
        {
            var tdb = Db.GetDefaultTestDb(out var stop0, out _, out var stop2, out _, out _, out _);
            var db = tdb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                null,
                TransferMetric.Factory,
                TransferMetric.ParetoCompare
            );


            var j = db.SelectProfile(profile)
                    .SelectStops(stop0, stop2)
                    .SelectTimeFrame(
                        db.EarliestDate(),
                        db.LatestDate())
                    .CalculateEarliestArrivalJourney()
                ;

            Assert.NotNull(j);
            Assert.Equal("https://example.com/connections/1", db.Connections.Get(j.Connection).GlobalId);
        }

        [Fact]
        public void EarliestArrivalJourney_SmallDb_RouteWithFirstMileWalk()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0", (50, 50.0)));
            var stop1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1",
                (0.000001, 0.00001))); // very walkable distance


            var w0 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/2", (50.00001, 50.00001)));

            writer.AddOrUpdateConnection(new Connection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0), 0));


            transitDb.CloseWriter();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);


            // Walk from start
            var journey = latest.SelectProfile(profile)
                .SelectStops(w0, stop1)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                .CalculateEarliestArrivalJourney();
            Assert.NotNull(journey);
        }


        [Fact]
        public void EarliestArrivalJourney_SingleConnectionDb_RouteWithLastMileWalk()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0", (50, 50.0)));
            var stop1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1",
                (0.000001, 0.00001))); // very walkable distance
            var w1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/3",
                (0.000002, 0.00002))); // very walkable distance

            writer.AddOrUpdateConnection(new Connection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0), 0));


            transitDb.CloseWriter();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);


            // Walk to end
            var journey = latest.SelectProfile(profile)
                .SelectStops(stop0, w1)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                .CalculateEarliestArrivalJourney();
            Assert.NotNull(journey);
        }

        [Fact]
        public void EarliestArrivalJourney_SingleConnectionDb_RouteWithFirstAndLastMileWalk()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0", (50, 50.0)));
            var w0 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/2", (50.00001, 50.00001)));
            var w1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/3",
                (0.000002, 0.00002))); // very walkable distance
            var stop1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1",
                (0.000001, 0.00001))); // very walkable distance

            writer.AddOrUpdateConnection(new Connection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0), 0));


            transitDb.CloseWriter();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);


            // Walk to end
            var journey = latest.SelectProfile(profile)
                .SelectStops(w0, w1)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                .CalculateEarliestArrivalJourney();
            Assert.NotNull(journey);
        }

        [Fact]
        public void EarliestArrivalJourney_FourConnectionsTransitDb_RouteWithIntermediateMileWalk_()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0", (50, 50.0)));
            var stop1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1", (0, 0.0)));
            var stop2 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/2",
                (0.000001, 0.00001))); // very walkable distance
            var stop3 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/3", (60.1, 60.1)));

            writer.AddOrUpdateConnection(new Connection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0), 0));

            writer.AddOrUpdateConnection(new Connection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 10, 00, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0), 0));
            writer.AddOrUpdateConnection(new Connection(stop2, stop3, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 10, 30, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 1), 0));

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(new Connection(stop0, stop1, "https://example.com/connections/2",
                new DateTime(2018, 12, 04, 20, 00, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 2), 0));

            transitDb.CloseWriter();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);
            var journey = latest.SelectProfile(profile)
                .SelectStops(stop0, stop3)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                .CalculateEarliestArrivalJourney();
            Assert.NotNull(journey);
            Assert.Equal(Journey<TransferMetric>.OTHERMODE, journey.PreviousLink.Connection);
            Assert.True(journey.PreviousLink.SpecialConnection);
        }


        [Fact]
        public void EarliestArrivalJourney_TwoConnectionTransitDb_RouteWithIntermediateMileWalk()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0", (0, 0.0)));
            var stop2 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1", (0.1, 0.1)));

            writer.AddOrUpdateConnection(new Connection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0), 0));

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(new Connection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 20, 00, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0), 0));

            transitDb.CloseWriter();
            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                null,
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);

            var sources = new List<StopId> {stop1};
            var targets = new List<StopId> {stop2};

            var latest = transitDb.Latest;

            var journey = latest
                    .SelectProfile(profile)
                    .SelectStops(sources, targets)
                    .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                        new DateTime(2018, 12, 04, 19, 00, 00, DateTimeKind.Utc))
                    .CalculateEarliestArrivalJourney()
                ;

            Assert.NotNull(journey);
            Assert.Equal(2, journey.AllParts().Count);
            Assert.Equal(new ConnectionId(0, 0), journey.Connection);
            Assert.False(journey.SpecialConnection);
            Assert.True(journey.PreviousLink.SpecialConnection);
            Assert.Equal((uint) (10 * 60), journey.Metric.TravelTime);
        }

        [Fact]
        public void EarliestArrivalJourney_TwoConnectionTransitDb_RouteWithOneConnection()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0", (0, 0.0)));
            var stop2 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1", (0.5, 0.5)));

            writer.AddOrUpdateConnection(new Connection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0), 0));

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(new Connection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 20, 00, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0), 0));

            transitDb.CloseWriter();
            var latest = transitDb.Latest;


            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                null,
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);


            var startTime = new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc);
            var sources = new List<StopId> {stop1};

            var targets = new List<StopId> {stop2};


            var journey = latest.SelectProfile(profile)
                    .SelectStops(sources, targets)
                    .SelectTimeFrame(startTime, new DateTime(2018, 12, 04, 19, 00, 00, DateTimeKind.Utc))
                    .CalculateEarliestArrivalJourney()
                ;

            Assert.NotNull(journey);
            Assert.Equal(2, journey.AllParts().Count);
            Assert.Equal(new ConnectionId(0, 0), journey.Connection);
            Assert.True(journey.PreviousLink.SpecialConnection);

            Assert.Equal((uint) 1, journey.Metric.NumberOfVehiclesTaken);
            Assert.Equal((uint) 10 * 60, journey.Metric.TravelTime);
        }

        [Fact]
        public void EarliestArrivalJourney_SmallTransitDb_JOurneysWithinTimeFrame()
        {
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0", (0, 0.0)));
            var stop2 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1", (0.1, 0.1)));

            writer.AddOrUpdateConnection(new Connection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0), 0));

            writer.AddOrUpdateConnection(new Connection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0), 0));
            writer.AddOrUpdateConnection(new Connection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 17, 20, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0), 0));
            writer.AddOrUpdateConnection(new Connection(stop1, stop2, "https://example.com/connections/2",
                new DateTime(2018, 12, 04, 18, 20, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0), 0));
            writer.AddOrUpdateConnection(new Connection(stop1, stop2, "https://example.com/connections/3",
                new DateTime(2018, 12, 04, 19, 20, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0), 0));
            writer.AddOrUpdateConnection(new Connection(stop1, stop2, "https://example.com/connections/4",
                new DateTime(2018, 12, 04, 20, 00, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0), 0));

            transitDb.CloseWriter();

            var latest = transitDb.Latest;
            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                null,
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);

            var journey = latest.SelectProfile(profile)
                .SelectStops(stop1, stop2)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 19, 00, 00, DateTimeKind.Utc))
                .CalculateEarliestArrivalJourney();
            Assert.NotNull(journey);
            Assert.Equal(2, journey.AllParts().Count);
        }

        [Fact]
        public void EarliestArrivalJourney_ConnectionsWithModes_TwoFailedRoutesOneSuccessfulRoute()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0", (50, 50.0)));
            var stop1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1", (0.0, 0)));
            var stop2 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/2", (5, 10)));


            writer.AddOrUpdateConnection(new Connection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 10, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0),
                Connection.ModeGetOnOnly));
            writer.AddOrUpdateConnection(new Connection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0),
                Connection.ModeGetOffOnly));

            transitDb.CloseWriter();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);

            var input = latest
                    .SelectProfile(profile)
                    .SelectStops(stop0, stop1)
                    .SelectTimeFrame(
                        new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                        new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                ;
            Assert.Null(input.CalculateEarliestArrivalJourney());

            input = latest
                    .SelectProfile(profile)
                    .SelectStops(stop1, stop0)
                    .SelectTimeFrame(
                        new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                        new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                ;
            Assert.Null(input.CalculateEarliestArrivalJourney());


            input = latest
                    .SelectProfile(profile)
                    .SelectStops(stop0, stop2)
                    .SelectTimeFrame(
                        new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                        new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                ;
            Assert.NotNull(input.CalculateEarliestArrivalJourney());
        }

        [Fact]
        public void EarliestArrivalJourney_ConnectionsWithNoGettingOff_NoRouteFound()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0", (50, 50.0)));
            var stop1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1", (0, 0.0)));
            var stop2 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/2", (0.001, 0.001)));
            var stop3 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/3", (60.1, 60.1)));

            // Note that all connections have mode '3', indicating neither getting on or of the connection
            writer.AddOrUpdateConnection(new Connection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 10, 00, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 0), 3));
            writer.AddOrUpdateConnection(new Connection(stop2, stop3, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 10, 30, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 1), 3));

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(new Connection(stop0, stop1, "https://example.com/connections/2",
                new DateTime(2018, 12, 04, 20, 00, 00, DateTimeKind.Utc), 10 * 60, new TripId(0, 2), 3));

            transitDb.CloseWriter();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);

            var journey = latest.SelectProfile(profile)
                .SelectStops(stop0, stop3)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 10, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                .CalculateEarliestArrivalJourney();

            // It is not possible to get on or off any connection
            // So we should not find anything
            Assert.Null(journey);
        }


        /// <summary>
        /// Another regression test from Kristof
        ///
        /// Earliest arrival scan sometimes selects the following:
        /// Departure at location A
        /// go to location B with train 0
        /// get on train 1
        /// go to A again, but stay seated
        /// continue to Destination
        /// 
        /// </summary>
        [Fact]
        public void EarliestArrivalJourney_TransitDbGoingViaStart_JourneyWithoutDetour()
        {
            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();

            var departure = wr.AddOrUpdateStop(new Stop("departure", (0.0, 0.0)));
            var arrival = wr.AddOrUpdateStop(new Stop("arrival", (1.0, 1.0)));
            var detour = wr.AddOrUpdateStop(new Stop("detour", (2.0, 2.0)));

            var trdetour = wr.AddOrUpdateTrip("tripDetour");
            var trdirect = wr.AddOrUpdateTrip("tripDirect");


            wr.AddOrUpdateConnection(new Connection(departure, detour, "a", 1000, 100, trdetour));
            wr.AddOrUpdateConnection(new Connection("b", detour, departure, 1500, 100, trdirect));
            wr.AddOrUpdateConnection(new Connection("c", departure, arrival, 1700, 100, trdirect));
            tdb.CloseWriter();

            var j = tdb.SelectProfile(new DefaultProfile())
                .SelectStops(departure, arrival)
                .SelectTimeFrame(1000, 2000)
                .CalculateEarliestArrivalJourney();

            // Only one connection should be used
            Assert.True(Equals(j.PreviousLink.Root, j.PreviousLink));
        }

        /// <summary>
        /// Regression test to test if journeys that are outside of the selected time frame aren't returned.
        /// </summary>
        [Fact]
        public void EarliestArrivalJourney_DepartureWalkOutOfWindow_NoJourneyFound()
        {
            // Locations: loc0 -> loc2

            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var loc0 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0",
                (3.1904983520507812, 51.256758449834216)));
            var loc1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1",
                (3.216590881347656, 51.197848510420464)));
            var loc2 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/2",
                (3.7236785888671875, 51.05348088381823)));

            writer.AddOrUpdateConnection(new Connection(loc1, loc2,
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 01, 00, DateTimeKind.Utc),
                30 * 60, new TripId(0, 0), 0));

            transitDb.CloseWriter();

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(10000),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);
            var journey = transitDb.SelectProfile(profile).SelectStops(loc0, loc2)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 17, 00, 00, DateTimeKind.Utc))
                .CalculateEarliestArrivalJourney();

            Assert.Null(journey);
        }

        /// <summary>
        /// Regression test to test if journeys that are outside of the selected time frame aren't returned.to mimick it
        /// 
        /// </summary>
        [Fact]
        public void EarliestArrivalJourney_ArrivalWalkOutOfWindow_NoJourneyFound()
        {
            // Locations: loc0 -> loc2

            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var loc0 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0",
                (3.1904983520507812, 51.256758449834216)));
            var loc1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1",
                (3.216590881347656, 51.197848510420464)));
            var loc2 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/2",
                (3.7236785888671875, 51.05348088381823)));

            writer.AddOrUpdateConnection(new Connection(loc2, loc1,
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 01, 00, DateTimeKind.Utc),
                30 * 60, new TripId(0, 0), 0));

            transitDb.CloseWriter();

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(10000),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);
            var journey = transitDb.SelectProfile(profile).SelectStops(loc2, loc0)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 17, 00, 00, DateTimeKind.Utc))
                .CalculateEarliestArrivalJourney();

            Assert.Null(journey);
        }

        /// <summary>
        /// This is a regression test, where the EAS would:
        ///
        /// Arrive in a big station and get out
        /// would take the train to a small, nearby station
        /// Take the train to the destination, passing the big station again
        /// </summary>
        [Fact]
        public void EarliestArrivalJourney_TdbWithTransferPossibility_NoLoops()
        {
            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();

            var departure = wr.AddOrUpdateStop(new Stop("departure", (0, 0)));
            var arrival = wr.AddOrUpdateStop(new Stop("arrival", (0, 0)));
            var bigstation = wr.AddOrUpdateStop(new Stop("bigstation", (0, 0)));
            var smallstation = wr.AddOrUpdateStop(new Stop("smallstation", (0, 0)));

            var tripA = wr.AddOrUpdateTrip("tripA");
            var tripB = wr.AddOrUpdateTrip("tripB");
            var tripC = wr.AddOrUpdateTrip("tripC");

            wr.AddOrUpdateConnection(new Connection(
                "0", departure, bigstation, 1000, 1000, tripA
            ));

            wr.AddOrUpdateConnection(new Connection(
                "1", bigstation, smallstation, 3000, 1000, tripB
            ));

            wr.AddOrUpdateConnection(new Connection(
                "2", smallstation, bigstation, 5000, 1000, tripC
            ));

            wr.AddOrUpdateConnection(new Connection(
                "3", bigstation, arrival, 6500, 1000, tripC
            ));
            tdb.CloseWriter();

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(0),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);
            var journey = tdb.SelectProfile(profile).SelectStops(departure, arrival)
                .SelectTimeFrame(0, 10000)
                .CalculateEarliestArrivalJourney();

            Assert.NotNull(journey);
        }
    }
}