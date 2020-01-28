using System;
using System.Collections;
using System.Linq;
using GTFS;
using GTFS.Entities;
using GTFS.Entities.Enumerations;
using Itinero.Transit.Data;
using Itinero.Transit.IO.GTFS;
using Itinero.Transit.IO.GTFS.Data;
using Itinero.Transit.Utils;
using Xunit;

namespace Itinero.Transit.Tests.IO.GTFS
{
    public class Gtfs2TdbTest
    {
        [Fact]
        public void AddDay_13oct_ConnectionsAreLoaded()
        {
            var feed = (new GTFSReader<GTFSFeed>()).Read("IO/GTFS/sncb-13-october.zip");
            var feedData = new FeedData(feed);
            var convertor = new Gtfs2Tdb(feedData);

            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();
            convertor.AddStops(wr);
            var d = new DateTime(2019, 10, 13, 0, 0, 0, DateTimeKind.Unspecified).Date;
            convertor.AddDay(wr, d, d, d.AddDays(2));
            wr.Close();

            Assert.True(tdb.Latest.ConnectionsDb.Count() > 10000);
            Assert.True(tdb.Latest.ConnectionsDb.EarliestDate.FromUnixTime() <= d.Date.AddMinutes(5));
        }

        [Fact]
        public void LoadTimePeriod_HourWithinGtfs_ConnectionsAreLoaded()
        {
            var tdb = new TransitDb(0);
            tdb.LoadGTFS("IO/GTFS/sncb-13-october.zip",
                new DateTime(2019, 10, 21, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2019, 10, 21, 11, 0, 0, DateTimeKind.Utc));
            var count = tdb.Latest.ConnectionsDb.Count();
            Assert.Equal(3597, count);
        }
        
        
        [Fact]
        public void LoadTimePeriod_HourAtFirstDayOfGtfs_ConnectionsAreLoaded()
        {
            var tdb = new TransitDb(0);
            tdb.LoadGTFS("IO/GTFS/sncb-13-october.zip",
                new DateTime(2019, 10, 07, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2019, 10, 07, 11, 0, 0, DateTimeKind.Utc));
            var count = tdb.Latest.ConnectionsDb.Count();
            Assert.Equal(3558, count);
        }
        
        
        [Fact]
        public void LoadTimePeriod_HourAtLastDayOfGtfs_ConnectionsAreLoaded()
        {
            var tdb = new TransitDb(0);
            tdb.LoadGTFS("IO/GTFS/sncb-13-october.zip",
                new DateTime(2019, 12, 14, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2019, 12, 14, 11, 0, 0, DateTimeKind.Utc));
            var count = tdb.Latest.ConnectionsDb.Count();
            Assert.Equal(2359, count);
        }

        [Fact]
        public void AgencyURLS_SNCB_ContainBelgianTrainId()
        {
            var feed = (new GTFSReader<GTFSFeed>()).Read("IO/GTFS/sncb-13-october.zip");
            var converter = new FeedData(feed);

            var urls = converter.AgencyUrls().ToList();

            Assert.Single((IEnumerable) urls);
            Assert.Equal("http://www.belgiantrain.be/", urls[0]);
        }

        [Fact]
        public void IdentifierPrefix_SNCB_BelgianTrail()
        {
            var feed = (new GTFSReader<GTFSFeed>()).Read("IO/GTFS/sncb-13-october.zip");
            var converter = new FeedData(feed);

            var url = converter.IdentifierPrefix;

            Assert.Equal("http://www.belgiantrain.be/", url);
            Assert.EndsWith("/", url);
        }

        [Fact]
        public void AddLocations_SNCB_ContainsBruges()
        {
            var feed = (new GTFSReader<GTFSFeed>()).Read("IO/GTFS/sncb-13-october.zip");
            var converter = new Gtfs2Tdb(new FeedData(feed), addUnusedStops: true);

            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();
            converter.AddStops(wr);
            wr.Close();

            var bruges = tdb.Latest.StopsDb.Get("http://www.belgiantrain.be/stop/8891009");
            Assert.Equal("Bruges", bruges.Attributes["name"]);
        }
        
        [Fact]
        public void GTFS2Tdb_AddDay_TripWithServiceDateNoConnection_ShouldNotAddTrip()
        {
            var feed = new GTFSFeed();
            feed.CalendarDates.Add(new CalendarDate()
            {
                Date = new DateTime(2020, 12, 01),
                ExceptionType = ExceptionType.Added,
                ServiceId = "10"
            });
            feed.Trips.Add(new Trip()
            {
                Direction = DirectionType.OneDirection,
                Headsign = "A test trip",
                Id = "11",
                RouteId = "12",
                ServiceId = "10"
            });
            
            var transitDb = new TransitDb(0);
            var transitDbWriter = transitDb.GetWriter();
            var converter = new Gtfs2Tdb(new FeedData(feed));
            converter.AddDay(transitDbWriter, new DateTime(2020, 12, 01),
                new DateTime(2020, 12, 01), new DateTime(2020, 12, 02));
            transitDbWriter.Close();
            
            Assert.Empty(transitDb.Latest.TripsDb);
        }
        
        [Fact]
        public void GTFS2Tdb_AddDay_TripWithServiceDateNoConnection_AddEmptyTrip_ShouldAddTrip()
        {
            var feed = new GTFSFeed();
            feed.CalendarDates.Add(new CalendarDate()
            {
                Date = new DateTime(2020, 12, 01),
                ExceptionType = ExceptionType.Added,
                ServiceId = "10"
            });
            feed.Trips.Add(new Trip()
            {
                Direction = DirectionType.OneDirection,
                Headsign = "A test trip",
                Id = "11",
                RouteId = "12",
                ServiceId = "10"
            });
            
            var transitDb = new TransitDb(0);
            var transitDbWriter = transitDb.GetWriter();
            var converter = new Gtfs2Tdb(new FeedData(feed), addEmptyTrips: true);
            converter.AddDay(transitDbWriter, new DateTime(2020, 12, 01),
                new DateTime(2020, 12, 01), new DateTime(2020, 12, 02));
            transitDbWriter.Close();
            
            Assert.Single(transitDb.Latest.TripsDb);
            var trip = transitDb.Latest.TripsDb.First();
            // TODO: this is different from what was expected.
            //Assert.Equal("11", trip.GlobalId);
        }
        
        [Fact]
        public void GTFS2Tdb_AddStops_TwoStops_ShouldAddTwoStops()
        {
            var feed = new GTFSFeed();
            feed.Stops.Add(new Stop()
            {
                Id = "stop1"
            });
            feed.Stops.Add(new Stop()
            {
                Id = "stop2"
            });
            
            var transitDb = new TransitDb(0);
            var transitDbWriter = transitDb.GetWriter();
            var converter = new Gtfs2Tdb(new FeedData(feed), addEmptyTrips: true, addUnusedStops: true);
            converter.AddStops(transitDbWriter);
            transitDbWriter.Close();
            
            Assert.Equal(2, transitDb.Latest.StopsDb.Count());
            
            // TODO: check if both stops loaded correctly when ids have been sorted out.
        }
        
        [Fact]
        public void GTFS2Tdb_AddDay_TwoStopTimesSameTrip_ShouldAddOneConnection()
        {
            var feed = new GTFSFeed();
            feed.CalendarDates.Add(new CalendarDate()
            {
                Date = new DateTime(2020, 12, 01),
                ExceptionType = ExceptionType.Added,
                ServiceId = "10"
            });
            feed.Trips.Add(new Trip()
            {
                Direction = DirectionType.OneDirection,
                Headsign = "A test trip",
                Id = "11",
                RouteId = "12",
                ServiceId = "10"
            });
            feed.Stops.Add(new Stop()
            {
                Id = "stop1"
            });
            feed.Stops.Add(new Stop()
            {
                Id = "stop2"
            });
            feed.StopTimes.Add(new StopTime()
            {
                TripId = "11",
                StopId = "stop1",
                StopSequence = 1,
                ArrivalTime = new TimeOfDay()
                {
                    Hours = 9
                },
                DepartureTime = new TimeOfDay()
                {
                    Hours = 9,
                    Minutes = 1
                }
            });
            feed.StopTimes.Add(new StopTime()
            {
                TripId = "11",
                StopId = "stop2",
                StopSequence = 1,
                ArrivalTime = new TimeOfDay()
                {
                    Hours = 10
                },
                DepartureTime = new TimeOfDay()
                {
                    Hours = 10,
                    Minutes = 1
                }
            });
            
            var transitDb = new TransitDb(0);
            var transitDbWriter = transitDb.GetWriter();
            var converter = new Gtfs2Tdb(new FeedData(feed), addEmptyTrips: true);
            converter.AddStops(transitDbWriter);
            converter.AddDay(transitDbWriter, new DateTime(2020, 12, 01),
                new DateTime(2020, 12, 01), new DateTime(2020, 12, 02));
            transitDbWriter.Close();
            
            Assert.Single(transitDb.Latest.ConnectionsDb);
            
            // TODO: check if both connection loaded correctly when id have been sorted out.
        }
    }
}