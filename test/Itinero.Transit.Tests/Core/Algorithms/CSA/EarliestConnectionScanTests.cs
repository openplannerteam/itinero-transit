using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Utils;
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

            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.1, 0.1);

            writer.AddOrUpdateConnection(
                stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 20, 00, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 1), 0);

            writer.Close();

            var latest = transitDb.Latest;
            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                null,
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);


            var journey = latest.SelectProfile(profile)
                .SelectStops(stop1, stop2)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 19, 00, 00, DateTimeKind.Utc))
                .EarliestArrivalJourney();

            Assert.NotNull(journey);
            Assert.Equal(2, journey.AllParts().Count());
        }

        [Fact]
        public void EarliestArrivalJourney_SmallDb_RouteFromStopToStop()
        {
            var tdb = Db.GetDefaultTestDb(out var stop0, out var stop1, out var stop2, out var _, out var _, out var _);
            var db = tdb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                null,
                TransferMetric.Factory,
                TransferMetric.ParetoCompare
            );


            var j = db
                .SelectProfile(profile)
                .SelectStops(stop0, stop1)
                .SelectTimeFrame(db.GetConn(0).DepartureTime.FromUnixTime(),
                    (db.GetConn(0).DepartureTime + 60 * 60 * 6).FromUnixTime())
                .EarliestArrivalJourney();

            Assert.NotNull(j);
            Assert.Equal(new ConnectionId(0, 0), j.Connection);


            j = db.SelectProfile(profile)
                    .SelectStops(stop0, stop2)
                    .SelectTimeFrame(
                        db.GetConn(0).DepartureTime.FromUnixTime(),
                        (db.GetConn(0).DepartureTime + 60 * 60 * 2).FromUnixTime())
                    .EarliestArrivalJourney()
                ;

            Assert.NotNull(j);
            Assert.Equal(new ConnectionId(0, 1), j.Connection);
        }

        [Fact]
        public void EarliestArrivalJourney_SmallDb_RouteWithFirstMileWalk()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.000001,
                0.00001); // very walkable distance


            var w0 = writer.AddOrUpdateStop("https://example.com/stops/2", 50.00001, 50.00001);

            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);


            writer.Close();

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
                .EarliestArrivalJourney();
            Assert.NotNull(journey);
        }


        [Fact]
        public void EarliestArrivalJourney_SingleConnectionDb_RouteWithLastMileWalk()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.000001,
                0.00001); // very walkable distance

            var w1 = writer.AddOrUpdateStop("https://example.com/stops/3", 0.000002, 0.00002); // very walkable distance

            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);


            writer.Close();

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
                .EarliestArrivalJourney();
            Assert.NotNull(journey);
        }

        [Fact]
        public void EarliestArrivalJourney_SingleConnectionDb_RouteWithFirstAndLastMileWalk()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.000001,
                0.00001); // very walkable distance
            var w0 = writer.AddOrUpdateStop("https://example.com/stops/2", 50.00001, 50.00001);

            var w1 = writer.AddOrUpdateStop("https://example.com/stops/3", 0.000002, 0.00002); // very walkable distance

            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);


            writer.Close();

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
                .EarliestArrivalJourney();
            Assert.NotNull(journey);
        }

        [Fact]
        public void EarliestArrivalJourney_FourConnectionsTransitDb_RouteWithIntermediateMileWalk_()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/2", 0.000001,
                0.00001); // very walkable distance
            var stop3 = writer.AddOrUpdateStop("https://example.com/stops/3", 60.1, 60.1);

            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);

            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 10, 00, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);
            writer.AddOrUpdateConnection(stop2, stop3, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 10, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 1), 0);

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/2",
                new DateTime(2018, 12, 04, 20, 00, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 2), 0);

            writer.Close();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);
            var journey = latest.SelectProfile(profile)
                .SelectStops(stop0, stop3)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                .EarliestArrivalJourney();
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

            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.1, 0.1);

            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 20, 00, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);

            writer.Close();
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
                    .EarliestArrivalJourney()
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

            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.5, 0.5);

            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 20, 00, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);

            writer.Close();
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
                    .EarliestArrivalJourney()
                ;

            Assert.NotNull(journey);
            Assert.Equal(2, journey.AllParts().Count);
            Assert.Equal(new ConnectionId(0, 0), journey.Connection);
            Assert.True(journey.PreviousLink.SpecialConnection);

            Assert.Equal((uint) 0, journey.Metric.NumberOfTransfers);
            Assert.Equal((uint) 10 * 60, journey.Metric.TravelTime);
        }

        [Fact]
        public void EarliestArrivalJourney_SmallTransitDb_JOurneysWithinTimeFrame()
        {
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.1, 0.1);

            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);

            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 17, 20, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/2",
                new DateTime(2018, 12, 04, 18, 20, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/3",
                new DateTime(2018, 12, 04, 19, 20, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/4",
                new DateTime(2018, 12, 04, 20, 00, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);

            writer.Close();

            var latest = transitDb.Latest;
            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                null,
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);

            var journey = latest.SelectProfile(profile)
                .SelectStops(stop1, stop2)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 19, 00, 00, DateTimeKind.Utc))
                .EarliestArrivalJourney();
            Assert.NotNull(journey);
            Assert.Equal(2, journey.AllParts().Count);
        }

        [Fact]
        public void EarliestArrivalJourney_ConnectionsWithModes_TwoFailedRoutesOneSuccessfulRoute()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.0, 0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/2", 5, 10);


            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 10, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0),
                Connection.ModeGetOnOnly);
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0),
                Connection.ModeGetOffOnly);

            writer.Close();

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
            Assert.Null(input.EarliestArrivalJourney());

            input = latest
                    .SelectProfile(profile)
                    .SelectStops(stop1, stop0)
                    .SelectTimeFrame(
                        new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                        new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                ;
            Assert.Null(input.EarliestArrivalJourney());


            input = latest
                    .SelectProfile(profile)
                    .SelectStops(stop0, stop2)
                    .SelectTimeFrame(
                        new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                        new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                ;
            Assert.NotNull(input.EarliestArrivalJourney());
        }

        [Fact]
        public void EarliestArrivalJourney_ConnectionsWithNoGettingOff_NoRouteFound()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/2", 0.001, 0.001); // very walkable distance
            var stop3 = writer.AddOrUpdateStop("https://example.com/stops/3", 60.1, 60.1);

            // Note that all connections have mode '3', indicating neither getting on or of the connection
            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 10, 00, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 3);
            writer.AddOrUpdateConnection(stop2, stop3, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 10, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 1), 3);

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/2",
                new DateTime(2018, 12, 04, 20, 00, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 2), 3);

            writer.Close();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);

            var journey = latest.SelectProfile(profile)
                .SelectStops(stop0, stop3)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 10, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                .EarliestArrivalJourney();

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

            var departure = wr.AddOrUpdateStop("departure", 0.0, 0.0);
            var arrival = wr.AddOrUpdateStop("arrival", 1.0, 1.0);

            var detour = wr.AddOrUpdateStop("detour", 2.0, 2.0);

            var trdetour = wr.AddOrUpdateTrip("tripDetour");
            var trdirect = wr.AddOrUpdateTrip("tripDirect");


            var c = new Connection
            {
                DepartureStop = departure,
                ArrivalStop = detour,
                DepartureTime = 1000,
                ArrivalTime = 1100,
                GlobalId = "a",
                TravelTime = 100,
                TripId = trdetour
            };

            wr.AddOrUpdateConnection(c);
            c = new Connection
            {
                DepartureStop = detour,
                ArrivalStop = departure,
                DepartureTime = 1500,
                ArrivalTime = 1600,
                GlobalId = "b",
                TravelTime = 100,
                TripId = trdirect
            };
            wr.AddOrUpdateConnection(c);

            c = new Connection
            {
                DepartureStop = departure,
                ArrivalStop = arrival,
                DepartureTime = 1700,
                ArrivalTime = 1800,
                GlobalId = "c",
                TravelTime = 100,
                TripId = trdirect
            };
            wr.AddOrUpdateConnection(c);
            wr.Close();

            var j = tdb.SelectProfile(new DefaultProfile())
                .SelectStops(departure, arrival)
                .SelectTimeFrame(1000, 2000)
                .EarliestArrivalJourney();

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

            var loc0 = writer.AddOrUpdateStop("https://example.com/stops/0", 3.1904983520507812,
                51.256758449834216);
            var loc1 = writer.AddOrUpdateStop("https://example.com/stops/1", 3.216590881347656,
                51.197848510420464);
            var loc2 = writer.AddOrUpdateStop("https://example.com/stops/2", 3.7236785888671875,
                51.05348088381823);

            writer.AddOrUpdateConnection(loc1, loc2,
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 01, 00, DateTimeKind.Utc),
                30 * 60, 0, 0, new TripId(0, 0), 0);

            writer.Close();

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(10000),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);
            var journey = transitDb.SelectProfile(profile).SelectStops(loc0, loc2)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 17, 00, 00, DateTimeKind.Utc))
                .EarliestArrivalJourney();

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

            var loc0 = writer.AddOrUpdateStop("https://example.com/stops/0", 3.1904983520507812,
                51.256758449834216);
            var loc1 = writer.AddOrUpdateStop("https://example.com/stops/1", 3.216590881347656,
                51.197848510420464);
            var loc2 = writer.AddOrUpdateStop("https://example.com/stops/2", 3.7236785888671875,
                51.05348088381823);

            writer.AddOrUpdateConnection(loc2, loc1,
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 01, 00, DateTimeKind.Utc),
                30 * 60, 0, 0, new TripId(0, 0), 0);

            writer.Close();

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(10000),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);
            var journey = transitDb.SelectProfile(profile).SelectStops(loc2, loc0)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 17, 00, 00, DateTimeKind.Utc))
                .EarliestArrivalJourney();

            Assert.Null(journey);
        }
    }
}