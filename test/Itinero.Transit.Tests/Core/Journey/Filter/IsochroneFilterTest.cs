using System;
using Itinero.Transit.Data;
using Itinero.Transit.Journey.Filter;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Utils;
using Xunit;

namespace Itinero.Transit.Tests.Core.Journey.Filter
{
    public class IsochroneFilterTest
    {
        [Fact]
        public void CreateIsochroneFilterTest()
        {
            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.000001,
                0.00001); // very walkable distance

            var connId = writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);

            writer.Close();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ProfileTransferCompare);

            var con = latest.ConnectionsDb.GetReader();
            con.MoveTo(connId);
            var iso = latest.SelectProfile(profile)
                .SelectSingleStop(stop0)
                .SelectTimeFrame(
                    new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                .IsochroneFrom();

            var filter = new IsochroneFilter<TransferMetric>(iso, true,
                new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc).ToUnixTime(),
                new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc).ToUnixTime());

            Assert.True(filter.CanBeTaken(con));
            Assert.False(filter.CanBeTaken(
                new SimpleConnection(1, "http://ex.org/con/563", stop1, stop0,
                    // This is the same time we depart from stop0 towards stop1
                    new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc).ToUnixTime(),
                    10 * 60, 0, 0, 0, new TripId(0, 1))));

            Assert.True(filter.CanBeTaken(
                new SimpleConnection(1, "http://ex.org/con/563", stop1, stop0,
                    // This is the same time we arrive at stop1
                    new DateTime(2018, 12, 04, 9, 40, 00, DateTimeKind.Utc).ToUnixTime(),
                    10 * 60, 0, 0, 0, new TripId(0, 1))));
        }

        [Fact]
        public void CreateIsochroneFilterTestArrival()
        {
            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.000001,
                0.00001); // very walkable distance

            var connId = writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);

            writer.Close();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ProfileTransferCompare);

            var iso = latest.SelectProfile(profile)
                .SelectSingleStop(stop1)
                .SelectTimeFrame(
                    new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                .IsochroneTo();

            var filter = new IsochroneFilter<TransferMetric>(iso, false,
                new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc).ToUnixTime(),
                new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc).ToUnixTime());

            var con = latest.ConnectionsDb.GetReader();
            con.MoveTo(connId);
            Assert.True(filter.CanBeTaken(con));

            // Arriving at stop0 at 09:30 makes that we could still just get our connection
            Assert.True(filter.CanBeTaken(
                new SimpleConnection(1, "http://ex.org/con/563",
                    stop1, stop0,
                    new DateTime(2018, 12, 04, 9, 20, 00, DateTimeKind.Utc).ToUnixTime(),
                    10 * 60, 0, 0, 0, new TripId(0, 1))));

            // If we arrived at 09:50 at stop0, we can't take our connection anymore
            Assert.False(filter.CanBeTaken(
                new SimpleConnection(1, "http://ex.org/con/563", stop1, stop0,
                    new DateTime(2018, 12, 04, 9, 40, 00, DateTimeKind.Utc).ToUnixTime(),
                    10 * 60, 0, 0, 0, new TripId(0, 1))));
        }

        [Fact]
        public void IsochroneFilterWithPcsTest()
        {
            // A small regression test
            // Build an isochronefilter, then calculate all journeys via PCS

            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.0,
                0.0); 

            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);

            writer.Close();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(), 
                TransferMetric.Factory,
                TransferMetric.ProfileTransferCompare);

            var input = latest
                    .SelectProfile(profile)
                    .SelectStops(stop0, stop1)
                    .SelectTimeFrame(
                        new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                        new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                ;
            input.IsochroneFrom();
            Assert.NotNull(input.Filter);
            Assert.True(input.Filter is IsochroneFilter<TransferMetric>);

            var journeys = input.AllJourneys();
            Assert.NotNull(journeys);
            Assert.Single(journeys);
        }
        
        
        [Fact]
        public void IsochroneFilterWithPcsWithSpecialModeTest()
        {
            // A small regression test for https://github.com/openplannerteam/itinero-transit/issues/63
            // Build an isochronefilter, then calculate all journeys via PCS

            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.0,
                0); 
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/2", 5,
                10);

            
            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 10, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 
                ConnectionExtensions.ModeGetOnOnly);
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 
                ConnectionExtensions.ModeGetOffOnly);

            writer.Close();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ProfileTransferCompare);

            var input = latest
                    .SelectProfile(profile)
                    .SelectStops(stop0, stop2)
                    .SelectTimeFrame(
                        new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                        new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc))
                ;

            var eas = input.EarliestArrivalJourney();
            Assert.NotNull(eas);
            input.ResetFilter();
            
            input.IsochroneFrom();
            Assert.NotNull(input.Filter);
            Assert.True(input.Filter is IsochroneFilter<TransferMetric>);

            var journeys = input.AllJourneys();
            Assert.NotNull(journeys);
            Assert.Single(journeys);
            
            
            
        }
    }
}