using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.IO.LC;
using Itinero.Transit.Journeys;
using Xunit;

namespace Itinero.Transit.Tests.Algorithm.CSA
{
    public class EarliestConnectionScanTests
    {
        [Fact]
        public void SimpleEasTest()
        {
            var tdb = Db.GetDefaultTestDb();
            var db = tdb.Latest;

            var profile = new Profile<TransferStats>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferStats.Factory,
                TransferStats.ProfileTransferCompare
            );

            var stopsReader = tdb.Latest.StopsDb.GetReader();

            stopsReader.MoveTo("https://example.com/stops/0");
            var stop0 = stopsReader.Id;
           
            stopsReader.MoveTo("https://example.com/stops/1");
            var stop1 = stopsReader.Id;

            stopsReader.MoveTo("https://example.com/stops/2");
            var stop2 = stopsReader.Id;

            
            var eas = new EarliestConnectionScan<TransferStats>(
                new ScanSettings<TransferStats>(
                    db,
                    stop0, stop1,
                    db.GetConn(0).DepartureTime.FromUnixTime(),
                    (db.GetConn(0).DepartureTime + 60 * 60 * 6).FromUnixTime(),
                    profile
                )
            );

            var j = eas.CalculateJourney();

            Assert.NotNull(j);
            Assert.Equal((uint) 0, j.Connection);


            eas = new EarliestConnectionScan<TransferStats>(new ScanSettings<TransferStats>(db,
                stop0,  stop2, db.GetConn(0).DepartureTime.FromUnixTime(),
                (db.GetConn(0).DepartureTime + 60 * 60 * 2).FromUnixTime(),
                profile
            ));

            j = eas.CalculateJourney();

            Assert.NotNull(j);
            Assert.Equal((uint) 1, j.Connection);
        }
        
        [Fact]
        public void SimpleNotGettingOffTest()
        {
            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/2", 0.001, 0.001); // very walkable distance
            var stop3 = writer.AddOrUpdateStop("https://example.com/stops/3", 60.1, 60.1);

            // Note that all connections have mode '3', indicating neither getting on or of the connection
            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 10, 00, 00), 10 * 60, 0, 0, 0, 3);
            writer.AddOrUpdateConnection(stop2, stop3, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 10, 30, 00), 10 * 60, 0, 0, 1, 3);

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/2",
                new DateTime(2018, 12, 04, 20, 00, 00), 10 * 60, 0, 0, 2, 3);

            writer.Close();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferStats>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferStats.Factory,
                TransferStats.ProfileTransferCompare);
            var eas = new EarliestConnectionScan<TransferStats>(new ScanSettings<TransferStats>(latest,
                stop0, stop3,
                new DateTime(2018, 12, 04, 10, 00, 00),
                new DateTime(2018, 12, 04, 11, 00, 00),
                profile));
            var journey = eas.CalculateJourney();
            
            // It is not possible to get on or off any connection
            // So we should not find anything
            Assert.Null(journey);
        }


        [Fact]
        public void EarliestConnectionScan_WithWalk()
        {
            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 50, 50.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/2", 0.001, 0.001); // very walkable distance
            var stop3 = writer.AddOrUpdateStop("https://example.com/stops/3", 60.1, 60.1);

            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 10, 00, 00), 10 * 60, 0, 0, 0, 0);
            writer.AddOrUpdateConnection(stop2, stop3, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 10, 30, 00), 10 * 60, 0, 0, 1, 0);

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(stop0, stop1, "https://example.com/connections/2",
                new DateTime(2018, 12, 04, 20, 00, 00), 10 * 60, 0, 0, 2, 0);

            writer.Close();

            var latest = transitDb.Latest;

            var profile = new Profile<TransferStats>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferStats.Factory,
                TransferStats.ProfileTransferCompare);
            var eas = new EarliestConnectionScan<TransferStats>(new ScanSettings<TransferStats>(latest,
                stop0, stop3,
                new DateTime(2018, 12, 04, 10, 00, 00),
                new DateTime(2018, 12, 04, 11, 00, 00),
                profile));
            var journey = eas.CalculateJourney();

            Assert.NotNull(journey);
            Assert.Equal(Journey<TransferStats>.WALK, journey.PreviousLink.PreviousLink.Connection);
            Assert.True(journey.PreviousLink.PreviousLink.SpecialConnection);
        }

        [Fact]
        public void EarliestConnectionScan_ShouldFindOneConnectionJourney()
        {
            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.1, 0.1);

            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00), 10 * 60, 0, 0, 0, 0);

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 20, 00, 00), 10 * 60, 0, 0, 0, 0);

            writer.Close();

            var latest = transitDb.Latest;
            var profile = new Profile<TransferStats>(new InternalTransferGenerator(),
                null,
                TransferStats.Factory,
                TransferStats.ProfileTransferCompare);
            var eas = new EarliestConnectionScan<TransferStats>(new ScanSettings<TransferStats>(latest,
                stop1, stop2, new DateTime(2018, 12, 04, 16, 00, 00),
                new DateTime(2018, 12, 04, 19, 00, 00),
                profile));
            var journey = eas.CalculateJourney();

            Assert.NotNull(journey);
            Assert.Equal(2, journey.AllParts().Count());
        }

        [Fact]
        public void EarliestConnectionScan_ShouldFindOneConnectionJourneyWithArrivalTravelTime()
        {
            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.1, 0.1);

            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00), 10 * 60, 0, 0, 0, 0);

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 20, 00, 00), 10 * 60, 0, 0, 0, 0);

            writer.Close();
            var profile = new Profile<TransferStats>(new InternalTransferGenerator(),
                null,
                TransferStats.Factory,
                TransferStats.ProfileTransferCompare);

            var sources = new List<((uint tileId, uint localId), Journey<TransferStats> journey)>
                {(stop1, null)};
            var targets = new List<((uint tileId, uint localId), Journey<TransferStats> journey)>
            {
                (stop2,
                    new Journey<TransferStats>(stop2, 0, profile.StatsFactory, 42)
                        .ChainSpecial(Journey<TransferStats>.WALK, 100, stop2, 0)
                )
            };

            var latest = transitDb.Latest;

            var settings = new ScanSettings<TransferStats>(
                latest,
                new DateTime(2018, 12, 04, 16, 00, 00),
                new DateTime(2018, 12, 04, 19, 00, 00),
                profile.StatsFactory, profile.ProfileComparator,
                profile.InternalTransferGenerator, profile.WalksGenerator,
                sources, targets
            );


            var eas = new EarliestConnectionScan<TransferStats>(settings);
            var journey = eas.CalculateJourney();

            Assert.NotNull(journey);
            Assert.Equal(3, journey.AllParts().Count);
            Assert.Equal(Journey<TransferStats>.WALK, journey.Connection);
            Assert.True(journey.SpecialConnection);
            Assert.False(journey.PreviousLink.SpecialConnection);
            Assert.Equal((uint) 0, journey.PreviousLink.Connection);
            Assert.Equal(100, journey.Stats.WalkingTime);
            Assert.Equal((uint) (10 * 60 + 100), journey.Stats.TravelTime);
        }

        [Fact]
        public void EarliestConnectionScan_ShouldFindOneConnectionJourneyWithDepartureTravelTime()
        {
            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.5, 0.5);

            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00), 10 * 60, 0, 0, 0, 0);

            // Prevent depletion of the DB
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 20, 00, 00), 10 * 60, 0, 0, 0, 0);

            writer.Close();
            var latest = transitDb.Latest;


            var profile = new Profile<TransferStats>(new InternalTransferGenerator(),
                null,
                TransferStats.Factory,
                TransferStats.ProfileTransferCompare);


            var startTime = new DateTime(2018, 12, 04, 16, 00, 00);
            var sources = new List<((uint tileId, uint localId), Journey<TransferStats> journey)>
            {
                (stop1,
                    new Journey<TransferStats>(stop1, startTime.ToUnixTime(), profile.StatsFactory, 42)
                        .ChainSpecial(Journey<TransferStats>.WALK, 
                            startTime.ToUnixTime() + 1000, stop1, tripId: 42 )
                )
            };

            var targets = new List<((uint tileId, uint localId), Journey<TransferStats> journey)>
                {(stop2, null)};


            var settings = new ScanSettings<TransferStats>(
                latest,
                startTime,
                new DateTime(2018, 12, 04, 19, 00, 00),
                profile.StatsFactory, profile.ProfileComparator,
                profile.InternalTransferGenerator, profile.WalksGenerator,
                sources, targets);


            var eas = new EarliestConnectionScan<TransferStats>(settings);
            var journey = eas.CalculateJourney();

            Assert.NotNull(journey);
            Assert.Equal(4, journey.AllParts().Count);
            Assert.Equal((uint) 0, journey.Connection);
            Assert.True(journey.PreviousLink.PreviousLink.SpecialConnection);
            Assert.Equal(Journey<TransferStats>.WALK,
                journey.PreviousLink.PreviousLink.Connection);
            
            Assert.Equal((uint) 1, journey.Stats.NumberOfTransfers);
            Assert.Equal(1000, journey.Stats.WalkingTime);
            Assert.Equal((uint) 30*60, journey.Stats.TravelTime);

        }

        [Fact]
        public void TestNoOverscan()
        {
            // build a one-connection db.
            var transitDb = new TransitDb();
            var writer = transitDb.GetWriter();

            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.1, 0.1);

            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00), 10 * 60, 0, 0, 0, 0);

            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00), 10 * 60, 0, 0, 0, 0);
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 17, 20, 00), 10 * 60, 0, 0, 0, 0);
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/2",
                new DateTime(2018, 12, 04, 18, 20, 00), 10 * 60, 0, 0, 0, 0);
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/3",
                new DateTime(2018, 12, 04, 19, 20, 00), 10 * 60, 0, 0, 0, 0);
            writer.AddOrUpdateConnection(stop1, stop2, "https://example.com/connections/4",
                new DateTime(2018, 12, 04, 20, 00, 00), 10 * 60, 0, 0, 0, 0);

            writer.Close();

            var latest = transitDb.Latest;
            var profile = new Profile<TransferStats>(new InternalTransferGenerator(),
                null,
                TransferStats.Factory,
                TransferStats.ProfileTransferCompare);
            var eas = new EarliestConnectionScan<TransferStats>(new ScanSettings<TransferStats>(
                latest,
                stop1, stop2,
                new DateTime(2018, 12, 04, 16, 00, 00),
                new DateTime(2018, 12, 04, 19, 00, 00),
                profile));
            var journey = eas.CalculateJourney();

            Assert.NotNull(journey);
            Assert.Equal(2, journey.AllParts().Count);
            Assert.Equal(new DateTime(2018, 12, 04, 16, 30, 00).ToUnixTime(), eas.ScanEndTime);
        }
    }
}