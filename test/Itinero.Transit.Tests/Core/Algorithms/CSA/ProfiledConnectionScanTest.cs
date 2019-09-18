using System;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Algorithms.Filter;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Xunit;

// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests.Core.Algorithms.CSA
{
    public class ProfiledConnectionScanTest
    {
        [Fact]
        public void AllJourneysTest_SingleConnectionTdb_JourneyWithBeginWalk()
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
            var journeys = latest.SelectProfile(profile)
                .SelectStops(w0, stop1)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                .CalculateAllJourneys();
            Assert.NotNull(journeys);
            Assert.Single(journeys);
        }


        [Fact]
        public void AllJourneysTest_SingleConnectionTdb_JourneyWithEndWalk()
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
            var journeys = latest.SelectProfile(profile)
                .SelectStops(stop0, w1)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                .CalculateAllJourneys();
            Assert.NotNull(journeys);
            Assert.Single(journeys);
        }

        [Fact]
        public void AllJourneysTest_SingleConnectionTdb_JourneyWithBeginAndEndWalk()
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
            var journeys = latest.SelectProfile(profile)
                .SelectStops(w0, w1)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                .CalculateAllJourneys();
            Assert.NotNull(journeys);
            Assert.Single(journeys);
        }


        [Fact]
        public void AllJourneysTest_SmallTdb_2Journeys()
        {
            var tdb = Db.GetDefaultTestDb(out var stop0, out _, out _, out var stop3, out var _, out var _);

            var db = tdb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(60),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory, TransferMetric.ParetoCompare);


            var journeys = db.SelectProfile(profile)
                .SelectStops(stop0, stop3)
                .SelectTimeFrame(
                    new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 18, 00, 00, DateTimeKind.Utc)
                ).CalculateAllJourneys();

            //Pr("---------------- DONE ----------------");
            foreach (var j in journeys)
            {
                //Pr(j.ToString());
                Assert.True(Equals(stop0, j.Root.Location));
                Assert.True(Equals(stop3, j.Location));
            }

            Assert.Equal(2, journeys.Count());
        }

        /// <summary>
        /// This test gives two possible routes to PCS:
        /// one which is clearly better then the other.
        /// </summary>
        [Fact]
        public static void AllJourneysTest_4ConnectionTdb_ExpectsOneOptimalJourney()
        {
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var loc0 = writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            var loc1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.1, 0.1);
            var loc2 = writer.AddOrUpdateStop("https://example.com/stops/1", 2.1, 0.1);
            var loc3 = writer.AddOrUpdateStop("https://example.com/stops/1", 3.1, 0.1);

            writer.AddOrUpdateConnection(loc0, loc1,
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                30 * 60, 0, 0, new TripId(0, 0), 0);


            writer.AddOrUpdateConnection(loc0, loc1,
                "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                40 * 60, 0, 0, new TripId(0, 1), 0);

            writer.AddOrUpdateConnection(loc2, loc3, "https//example.com/connections/2",
                new DateTime(2018, 12, 04, 20, 00, 00, DateTimeKind.Utc),
                40 * 60, 0, 0, new TripId(0, 2), 0);

            writer.AddOrUpdateConnection(loc2, loc3, "https//example.com/connections/4",
                new DateTime(2018, 12, 04, 2, 00, 00, DateTimeKind.Utc),
                40 * 60, 0, 0, new TripId(0, 3), 0);

            writer.Close();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(
                new InternalTransferGenerator(60),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);

            var journeys = latest.SelectProfile(profile)
                .SelectStops(loc0, loc1)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 18, 00, 00, DateTimeKind.Utc))
                .CalculateAllJourneys();
            Assert.Single(journeys);
            foreach (var j in journeys)
            {
                Assert.Equal(30 * 60, (int) j.Metric.TravelTime);
            }
        }

        [Fact]
        public static void AllJourneysTest_4ConnectionTdbWithMetricGuesser_ExpectsOneOptimalJourney()
        {
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var loc0 = writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            var loc1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.1, 0.1);
            var loc2 = writer.AddOrUpdateStop("https://example.com/stops/1", 2.1, 0.1);
            var loc3 = writer.AddOrUpdateStop("https://example.com/stops/1", 3.1, 0.1);

            writer.AddOrUpdateConnection(loc0, loc1,
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                30 * 60, 0, 0, new TripId(0, 0), 0);


            writer.AddOrUpdateConnection(loc0, loc1,
                "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                40 * 60, 0, 0, new TripId(0, 1), 0);

            writer.AddOrUpdateConnection(loc2, loc3, "https//example.com/connections/2",
                new DateTime(2018, 12, 04, 20, 00, 00, DateTimeKind.Utc),
                40 * 60, 0, 0, new TripId(0, 2), 0);

            writer.AddOrUpdateConnection(loc2, loc3, "https//example.com/connections/4",
                new DateTime(2018, 12, 04, 2, 00, 00, DateTimeKind.Utc),
                40 * 60, 0, 0, new TripId(0, 3), 0);

            writer.Close();


            var latest = transitDb.Latest;


            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(60),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory, TransferMetric.ParetoCompare);

            var calculator = latest.SelectProfile(profile)
                .SelectStops(loc0, loc1)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 18, 00, 00, DateTimeKind.Utc));


            var settings = calculator.GetScanSettings();

            settings.MetricGuesser = new SimpleMetricGuesser<TransferMetric>(
                calculator.ConnectionEnumerator,
                calculator.From[0]
            );

            var pcs = new ProfiledConnectionScan<TransferMetric>(calculator.GetScanSettings());
            var journeys = pcs.CalculateJourneys();
            Assert.Single(journeys);
            foreach (var j in journeys)
            {
                Assert.Equal(30 * 60, (int) j.Metric.TravelTime);
            }
        }


        [Fact]
        public void AllJourneysTest_1ConnectionTdbWithNoGettingOfMode_ExpectsNoJourneys()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.1, 0.1);

            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0),
                3); // MODE 3 - cant get on or off

            writer.Close();
            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);
            var journey = latest.SelectProfile(profile)
                .SelectStops(stop1, stop2)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 19, 00, 00, DateTimeKind.Utc))
                .CalculateAllJourneys();

            Assert.Null(journey);
        }


        /// <summary>
        /// Regression test
        ///
        ///
        /// Kristof discovered a case where a huge crows flight took 7h and fell squarely out of the search window,
        /// even though other options were still available
        ///
        /// THis test tries to mimick it
        /// 
        /// </summary>
        [Fact]
        public void AllJourneysTest_1ConnectionTdbWalkRequired_ExpectsNoJourneyAsWalkFallsBeforeTimeWindow()
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
            var journeys = transitDb.SelectProfile(profile).SelectStops(loc0, loc2)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 17, 00, 00, DateTimeKind.Utc))
                .CalculateAllJourneys();

            Assert.Null(journeys);
        }

        /// <summary>
        /// Regression test
        ///
        ///
        /// Kristof discovered a case where a huge crows flight took 7h and fell squarely out of the search window,
        /// even though other options were still available
        ///
        /// THis test tries to mimick it
        /// 
        /// </summary>
        [Fact]
        public void AllJourneysTest_1ConnectionTdbWalkRequired_ExpectsNoJourneyAsWalkFallsAfterTimeWindow()
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
            var journeys = transitDb.SelectProfile(profile).SelectStops(loc2, loc0)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 17, 00, 00, DateTimeKind.Utc))
                .CalculateAllJourneys();

            Assert.Null(journeys);
        }

        [Fact]
        public void AllJourneysTest_SingleConnectionTdb_JourneyWithNoWalkAndNoSearch()
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

            var profile = new Profile<TransferMetric>(
                new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(0),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);


            // Walk from start
            var journeys = latest.SelectProfile(profile)
                .SelectStops(stop0, stop1)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                .CalculateAllJourneys();
            Assert.NotNull(journeys);
            Assert.Single(journeys);
        }


        [Fact]
        public void AllJourneysTest_TwoConnectionDifferentTrip_JourneyWithTransfer()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.000001,
                0.00001); // very walkable distance
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/2", 0.08, 0.00001); // very walkable distance


            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);

            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 10, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 1), 0);

            writer.Close();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(
                new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(0),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);


            // Walk from start
            var journeys = latest.SelectProfile(profile)
                .SelectStops(stop0, stop2)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 12, 00, 00, DateTimeKind.Utc))
                .CalculateAllJourneys();
            Assert.NotNull(journeys);
            Assert.Single(journeys);
            Assert.Equal((uint) 1, journeys[0].Metric.NumberOfTransfers);
        }


        [Fact]
        public void AllJourneysTest_TwoConnectionSameTrip_JourneyWithExtendedTrip()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.000001,
                0.00001); // very walkable distance
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/2", 0.08, 0.00001); // very walkable distance


            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);

            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 10, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);

            writer.Close();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(
                new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(0),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);


            // Walk from start
            var journeys = latest.SelectProfile(profile)
                .SelectStops(stop0, stop2)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 12, 00, 00, DateTimeKind.Utc))
                .CalculateAllJourneys();
            Assert.NotNull(journeys);
            Assert.Single(journeys);
            Assert.Equal((uint) 0, journeys[0].Metric.NumberOfTransfers);

        }
    }
}