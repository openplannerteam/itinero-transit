using System;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Xunit;

// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests.Algorithm.CSA
{
    public class ProfiledConnectionScanTest
    {
        [Fact]
        public void TestPcsSimple()
        {
            var tdb = Db.GetDefaultTestDb(out var stop0, out _, out _, out var stop3, out var _, out var _);

            var db = tdb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(60),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory, TransferMetric.ProfileTransferCompare);


            var journeys = db.SelectProfile(profile)
                .SelectStops(stop0, stop3)
                .SelectTimeFrame(
                    new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 18, 00, 00, DateTimeKind.Utc)
                ).AllJourneys();

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
        public static void TestFiltering()
        {
            var transitDb = new TransitDb();
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
                TransferMetric.Factory, TransferMetric.ProfileTransferCompare);

            var journeys = latest.SelectProfile(profile)
                .SelectStops(loc0, loc1)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 18, 00, 00, DateTimeKind.Utc))
                .AllJourneys();
            Assert.Single(journeys);
            foreach (var j in journeys)
            {
                Assert.Equal(30 * 60, (int) j.Metric.TravelTime);
            }
        }

        [Fact]
        public void ShouldFindNoConnectionJourney()
        {
            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.1, 0.1);

            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0),
                3); // MODE 3 - cant get on or off

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 20, 00, 00, DateTimeKind.Utc), 10 * 60, 0, 0, new TripId(0, 0), 3);
            writer.Close();
            var latest = transitDb.Latest;

            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ProfileTransferCompare);
            var journey = latest.SelectProfile(profile)
                .SelectStops(stop1, stop2)
                .SelectTimeFrame(new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc),
                    new DateTime(2018, 12, 04, 19, 00, 00, DateTimeKind.Utc))
                .AllJourneys();

            Assert.Null(journey);
        }
    }
}