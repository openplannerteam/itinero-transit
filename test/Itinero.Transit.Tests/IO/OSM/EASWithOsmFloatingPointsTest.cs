using System;
using Itinero.Transit.Data;
using Itinero.Transit.IO.OSM.Data;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Xunit;

namespace Itinero.Transit.Tests.IO.OSM
{
    public class EasWithOsmFloatingPointsTest
    {
        [Fact]
        public void WithOsmWalk()
        {
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.000001,
                0.00001); // very walkable distance


            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);

            writer.Close();

            var input = transitDb
                .SelectProfile(new DefaultProfile())
                .SelectStops((50.0, 50.0), (0.0, 0.0))
                .SelectTimeFrame(
                    new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 10, 00, 00, DateTimeKind.Utc));

            var eas = input.EarliestArrivalJourney();
            Assert.NotNull(eas);

        }


        [Fact]
        public void WithBeginOsmWalk()
        {
            // build a one-connection db.
            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.000001,
                0.00001); // very walkable distance


            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);

            writer.Close();

            var departureLocation = "https://www.openstreetmap.org/#map=19/50.00005/49.99953";

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ProfileTransferCompare);

            var osmStopReader = new OsmLocationStopReader(1);

            osmStopReader.MoveTo(departureLocation);
            osmStopReader.AddSearchableLocation(osmStopReader.Id);

            // Walk from start
            var input = latest
                .SelectProfile(profile)
                .AddStopsReader(osmStopReader)
                .SelectStops(departureLocation, "https://example.com/stops/1")
                .SelectTimeFrame(new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc));


            var journey = input.EarliestArrivalJourney();
            Assert.NotNull(journey);
            input.ResetFilter();
            var las = input.LatestDepartureJourney();
            Assert.NotNull(las);
            input.ResetFilter();
            var pcs = input.AllJourneys();
            Assert.NotNull(pcs);
            Assert.Single(pcs);
        }


        [Fact]
        public void WithEndWalk()
        {
            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.000001,
                0.00001); // very walkable distance

            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);


            writer.Close();

            var latest = transitDb.Latest;
            var arrivalLocation = "https://www.openstreetmap.org/#map=19/0.0/0.0";


            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ProfileTransferCompare);

            var osmStopReader = new OsmLocationStopReader(1);

            osmStopReader.MoveTo(arrivalLocation);
            osmStopReader.AddSearchableLocation(osmStopReader.Id);

            // Walk to end

            var input = latest
                .SelectProfile(profile)
                .AddStopsReader(osmStopReader)
                .SelectStops("https://example.com/stops/0", arrivalLocation)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc));


            var journey = input.EarliestArrivalJourney();
            Assert.NotNull(journey);
            input.ResetFilter();
            var las = input.LatestDepartureJourney();
            Assert.NotNull(las);
            input.ResetFilter();
            var pcs = input.AllJourneys();
            Assert.NotNull(pcs);
            Assert.Single(pcs);
        }

        [Fact]
        public void WithStartEndWalk()
        {
            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.000001,
                0.00001); // very walkable distance

            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 9, 30, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 0);


            writer.Close();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ProfileTransferCompare);

            var departureLocation = "https://www.openstreetmap.org/#map=19/50.00005/49.99953";
            var arrivalLocation = "https://www.openstreetmap.org/#map=19/0.0/0.0";

            var osmStopReader = new OsmLocationStopReader(1);
            osmStopReader.MoveTo(departureLocation);
            osmStopReader.AddSearchableLocation(osmStopReader.Id);
            osmStopReader.MoveTo(arrivalLocation);
            osmStopReader.AddSearchableLocation(osmStopReader.Id);


            var input = latest
                .SelectProfile(profile)
                .AddStopsReader(osmStopReader)
                .SelectStops(departureLocation, arrivalLocation)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 9, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 11, 00, 00, DateTimeKind.Utc));


            var journey = input.EarliestArrivalJourney();
            Assert.NotNull(journey);
            input.ResetFilter();
            var las = input.LatestDepartureJourney();
            Assert.NotNull(las);
            input.ResetFilter();
            var pcs = input.AllJourneys();
            Assert.NotNull(pcs);
            Assert.Single(pcs);
        }
    }
}