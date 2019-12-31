using System;
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
    public class LatestConnectionScanTests
    {
        [Fact]
        public void LatestConnectionScan_SmallTdb_ExpectsJourney()
        {
            var tdb = Db.GetDefaultTestDb(out var stop0, out var stop1, out var stop2, out _, out _, out _);
            var db = tdb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(0),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare
            );

            var j =
                db.SelectProfile(profile)
                    .SelectStops(stop0, stop1)
                    .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00),
                        new DateTime(2018, 12, 04, 18, 00, 00))
                    .CalculateLatestDepartureJourney();
                
            Assert.NotNull(j);
            Assert.Equal(new ConnectionId(0,0), j.Connection);

            j = db.SelectProfile(profile)
                .SelectStops(stop0, stop2)
                .SelectTimeFrame(db.GetConn(0).DepartureTime.FromUnixTime(),
                    (db.GetConn(0).DepartureTime + 60 * 60 * 2).FromUnixTime())
                .CalculateLatestDepartureJourney();
                
                
            Assert.NotNull(j);
            Assert.Equal(j.Root.Location, stop0);
            Assert.Equal(j.Location, stop2);
        }


        [Fact]
        public void LatestConnectionScan_ShouldFindNoConnectionJourney()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0", (0.0, 0.0)));
            var stop2 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1", (0.1, 0.1)));

            writer.AddOrUpdateConnection(new Connection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 3)); // MODE 3 - cant get on or off

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(new Connection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 20, 00, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 3));
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
                .CalculateLatestDepartureJourney();

            Assert.Null(journey);
        }

        [Fact]
        public void LatestConnectionScan_ShouldFindOneConnectionJourney()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0", (0.0, 0.0)));
            var stop2 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1", (0.1, 0.1)));

            writer.AddOrUpdateConnection(new Connection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0));

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(new Connection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 20, 00, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0));
            writer.Close();
            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);
            var journey = latest.SelectProfile(profile)
                .SelectStops(stop1, stop2)
                .SelectTimeFrame(        new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 19, 00, 00, DateTimeKind.Utc))
                .CalculateLatestDepartureJourney();

            Assert.NotNull(journey);
            Assert.Equal(2, journey.AllParts().Count());
        }
        
        [Fact]
        public void Latest_ConnectionScan_WithBeginWalk()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0", (50, 50.0)));
            var stop1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1", (0.000001, 0.00001))); // very walkable distance


            var w0 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/2", (50.00001, 50.00001)));
            writer.AddOrUpdateStop(new Stop("https://example.com/stops/3", (0.00002, 0.00002)));

            writer.AddOrUpdateConnection(new Connection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0));


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
                .CalculateLatestDepartureJourney();
            Assert.NotNull(journey);
        }
        
        [Fact]
        public void Latest_ConnectionScan_WithEndWalk()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0", (50, 50.0)));
            var stop1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1", (0.000001, 0.00001))); // very walkable distance


            writer.AddOrUpdateStop(new Stop("https://example.com/stops/2", (50.00001, 50.00001)));
            var w1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/3", (0.00002, 0.00002)));

            writer.AddOrUpdateConnection(new Connection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0));


            writer.Close();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);


            // Walk from start
            var journey = latest.SelectProfile(profile)
                .SelectStops(stop0, w1)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                .CalculateLatestDepartureJourney();
            Assert.NotNull(journey);
        }
        
        [Fact]
        public void Latest_ConnectionScan_WithBeginEndWalk()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0", (50, 50.0)));
            var stop1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1", (0.000001, 0.00001))); // very walkable distance


            var w0 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/2", (50.00001, 50.00001)));
            var w1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/3", (0.000020, 0.000020)));

            writer.AddOrUpdateConnection(new Connection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0));


            writer.Close();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);


            // Walk from start
            var journey = latest.SelectProfile(profile)
                .SelectStops(w0, w1)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                .CalculateLatestDepartureJourney();
            Assert.NotNull(journey);
        }

        /// <summary>
        /// Regression test to test if journeys that are outside of the selected time frame aren't returned.
        /// </summary>
        [Fact]
        public void Latest_ConnectionScan_DepartureWalkOutOfWindow_NoJourneyFound()
        {
            // Locations: loc0 -> loc2

            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var loc0 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0", (3.1904983520507812,51.256758449834216)));
            var loc1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1", (3.2165908813476560,51.197848510420464)));
            var loc2 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/2", (3.7236785888671875,51.053480883818230)));

            writer.AddOrUpdateConnection(new Connection(loc1, loc2,
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 01, 00, DateTimeKind.Utc),
                30 * 60, 0, 0, new TripId(0, 0), 0));
            
            writer.Close();
            
            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(10000),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);
            var journey = transitDb.SelectProfile(profile).SelectStops(loc0, loc2)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 17, 00, 00, DateTimeKind.Utc))
                .CalculateLatestDepartureJourney();
            
            Assert.Null(journey);
        }

        /// <summary>
        /// Regression test to test if journeys that are outside of the selected time frame aren't returned.to mimick it
        /// 
        /// </summary>
        [Fact]
        public void Latest_ConnectionScan_ArrivalWalkOutOfWindow_NoJourneyFound()
        {
            // Locations: loc0 -> loc2

            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var loc0 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/0", (3.1904983520507812,51.256758449834216)));
            var loc1 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/1", (3.2165908813476560,51.197848510420464)));
            var loc2 = writer.AddOrUpdateStop(new Stop("https://example.com/stops/2", (3.7236785888671875,51.053480883818230)));

            writer.AddOrUpdateConnection(new Connection(loc2, loc1,
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 01, 00, DateTimeKind.Utc),
                30 * 60, 0, 0, new TripId(0, 0), 0));
            
            writer.Close();
            
            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(10000),
                TransferMetric.Factory,
                TransferMetric.ParetoCompare);
            var journey = transitDb.SelectProfile(profile).SelectStops(loc2, loc0)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 17, 00, 00, DateTimeKind.Utc))
                .CalculateLatestDepartureJourney();
            
            Assert.Null(journey);
        }
        
    }
}