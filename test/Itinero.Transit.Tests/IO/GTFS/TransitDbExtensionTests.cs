using System;
using System.Linq;
using GTFS;
using GTFS.Entities;
using GTFS.Entities.Enumerations;
using Itinero.Transit.Data;
using Itinero.Transit.IO.GTFS;
using Xunit;

namespace Itinero.Transit.Tests.IO.GTFS
{
    public class TransitDbExtensionTests
    {
        [Fact]
        public void TransitDbExtensions_AddAgencies_OneAgency_OneOperator()
        {
            var feed = new GTFSFeed();
            feed.Agencies.Add(new Agency()
            {
                Id = "NMBS/SNCB",
                Name= "NMBS/SNCB",
                URL = "http://www.belgiantrain.be/",
                Timezone = "Europe/Brussel",
                LanguageCode = "fr"
            });

            var transitDb = new TransitDb(0);
            var writer = transitDb.GetWriter();
            writer.AddAgencies(feed, out _, out _);
            writer.Close();

            Assert.Single(transitDb.Latest.OperatorDb);
            Assert.Equal("http://www.belgiantrain.be/", transitDb.Latest.OperatorDb.First().GlobalId);
        }

        [Fact]
        public void TransitDbExtensions_LoadGTFS_OneStopOnly_DefaultSettings_NoStopsLoad()
        {
            // stop_id,stop_code,stop_name,stop_desc,stop_lat,stop_lon,zone_id,stop_url,location_type,parent_station,platform_code
            // 8814001,,Bruxelles-Midi,,50.8357100,4.33653000,,,0,S8814001,
            var feed = new GTFSFeed();
            feed.Stops.Add(new Stop()
            {
                Id = "8814001",
                Code = string.Empty,
                Name = "Bruxelles-Midi",
                Description = string.Empty,
                Latitude = 50.8357100,
                Longitude = 4.33653000,
                Zone = string.Empty,
                Url = string.Empty,
                LocationType = LocationType.Stop,
                ParentStation = "S8814001",
                PlatformCode = string.Empty
            });

            var transitDb = new TransitDb(0);
            transitDb.LoadGTFS(feed, 
                new DateTime(2020,01,01), new DateTime(2020,01,01));

            Assert.Empty(transitDb.Latest.StopsDb);
        }

        [Fact]
        public void TransitDbExtensions_LoadGTFS_OneStopOnly_LoadUnusedStops_OneStopLoaded()
        {
            // stop_id,stop_code,stop_name,stop_desc,stop_lat,stop_lon,zone_id,stop_url,location_type,parent_station,platform_code
            // 8814001,,Bruxelles-Midi,,50.8357100,4.33653000,,,0,S8814001,
            var feed = new GTFSFeed();
            feed.Stops.Add(new Stop()
            {
                Id = "8814001",
                Code = string.Empty,
                Name = "Bruxelles-Midi",
                Description = string.Empty,
                Latitude = 50.8357100,
                Longitude = 4.33653000,
                Zone = string.Empty,
                Url = string.Empty,
                LocationType = LocationType.Stop,
                ParentStation = "S8814001",
                PlatformCode = string.Empty
            });

            var transitDb = new TransitDb(0);
            transitDb.LoadGTFS(feed, 
                new DateTime(2020,01,01), new DateTime(2020,01,01),
                new GTFSLoadSettings()
                {
                    AddUnusedStops = true
                });

            Assert.Single(transitDb.Latest.StopsDb);
        }

        [Fact]
        public void TransitDbExtensions_LoadGTFS_TwoStops_Used_TwoStopsLoaded()
        {
            // stop_id,stop_code,stop_name,stop_desc,stop_lat,stop_lon,zone_id,stop_url,location_type,parent_station,platform_code
            // 8814001,,Bruxelles-Midi,,50.8357100,4.33653000,,,0,S8814001,
            // 8814118,,Forest-Est,,50.8102000,4.32094000,,,0,S8814118,
            
            var feed = new GTFSFeed();
            feed.Stops.Add(new Stop()
            {
                Id = "8814001",
                Code = string.Empty,
                Name = "Bruxelles-Midi",
                Description = string.Empty,
                Latitude = 50.8357100,
                Longitude = 4.33653000,
                Zone = string.Empty,
                Url = string.Empty,
                LocationType = LocationType.Stop,
                ParentStation = "S8814001",
                PlatformCode = string.Empty
            });
            feed.Stops.Add(new Stop()
            {
                Id = "8814118",
                Code = string.Empty,
                Name = "Forest-Est",
                Description = string.Empty,
                Latitude = 50.8102000,
                Longitude = 4.32094000,
                Zone = string.Empty,
                Url = string.Empty,
                LocationType = LocationType.Stop,
                ParentStation = "S8814118",
                PlatformCode = string.Empty
            });
            
            // service_id,monday,tuesday,wednesday,thursday,friday,saturday,sunday,start_date,end_date
            // 000054,0,0,0,0,0,0,0,20200125,20201213
            feed.Calendars.Add(new Calendar()
            {
                ServiceId = "000054",
                Monday = true,
                Tuesday = true,
                Wednesday = true,
                Thursday = true,
                Friday = true,
                Saturday = true,
                Sunday = true,
                StartDate = new DateTime(2020,01,01),
                EndDate = new DateTime(2020,12,31)
            });
            
            // route_id,service_id,trip_id,trip_headsign,trip_short_name,direction_id,block_id,shape_id,trip_type
            // 32,000054,88____:049::8814258:8814001:10:652:20200208,Bruxelles-Midi,11105,,709,,1
            feed.Trips.Add(new Trip()
            {
                Id = "88____:049::8814258:8814001:10:652:20200208",
                RouteId = "32",
                ServiceId = "000054",
                Headsign = "Bruxelles-Midi",
                ShortName = "11105",
                Direction = null,
                BlockId = "709",
                ShapeId = null
            });
            
            // trip_id,arrival_time,departure_time,stop_id,stop_sequence,stop_headsign,pickup_type,drop_off_type,shape_dist_traveled
            // 88____:049::8814258:8814001:10:652:20200208,06:46:00,06:46:00,8814118,9,,1,1,
            // 88____:049::8814258:8814001:10:652:20200208,06:52:00,06:52:00,8814001,10,,1,0,
            feed.StopTimes.Add(new StopTime()
            {
                TripId= "88____:049::8814258:8814001:10:652:20200208",
                ArrivalTime = new TimeOfDay()
                {
                    Hours = 6,
                    Minutes = 46,
                    Seconds = 0
                },
                DepartureTime = new TimeOfDay()
                {
                    Hours = 6,
                    Minutes = 46,
                    Seconds = 0
                },
                StopId = "8814118",
                StopSequence = 9,
                StopHeadsign = null,
                PickupType = PickupType.Regular,
                DropOffType = DropOffType.Regular,
                ShapeDistTravelled = null
            });
            feed.StopTimes.Add(new StopTime()
            {
                TripId= "88____:049::8814258:8814001:10:652:20200208",
                ArrivalTime = new TimeOfDay()
                {
                    Hours = 6,
                    Minutes = 52,
                    Seconds = 0
                },
                DepartureTime = new TimeOfDay()
                {
                    Hours = 6,
                    Minutes = 52,
                    Seconds = 0
                },
                StopId = "8814001",
                StopSequence = 10,
                StopHeadsign = null,
                PickupType = PickupType.Regular,
                DropOffType = DropOffType.NoPickup,
                ShapeDistTravelled = null
            });

            var transitDb = new TransitDb(0);
            transitDb.LoadGTFS(feed, 
                new DateTime(2020,01,01), new DateTime(2020,01,01));

            Assert.Equal(2, transitDb.Latest.StopsDb.Count);
        }

        [Fact]
        public void TransitDbExtensions_LoadGTFS_TwoStopTimes_Used_OneConnectionLoaded()
        {
            // stop_id,stop_code,stop_name,stop_desc,stop_lat,stop_lon,zone_id,stop_url,location_type,parent_station,platform_code
            // 8814001,,Bruxelles-Midi,,50.8357100,4.33653000,,,0,S8814001,
            // 8814118,,Forest-Est,,50.8102000,4.32094000,,,0,S8814118,
            
            var feed = new GTFSFeed();
            feed.Stops.Add(new Stop()
            {
                Id = "8814001",
                Code = string.Empty,
                Name = "Bruxelles-Midi",
                Description = string.Empty,
                Latitude = 50.8357100,
                Longitude = 4.33653000,
                Zone = string.Empty,
                Url = string.Empty,
                LocationType = LocationType.Stop,
                ParentStation = "S8814001",
                PlatformCode = string.Empty
            });
            feed.Stops.Add(new Stop()
            {
                Id = "8814118",
                Code = string.Empty,
                Name = "Forest-Est",
                Description = string.Empty,
                Latitude = 50.8102000,
                Longitude = 4.32094000,
                Zone = string.Empty,
                Url = string.Empty,
                LocationType = LocationType.Stop,
                ParentStation = "S8814118",
                PlatformCode = string.Empty
            });
            
            // service_id,monday,tuesday,wednesday,thursday,friday,saturday,sunday,start_date,end_date
            // 000054,0,0,0,0,0,0,0,20200125,20201213
            feed.Calendars.Add(new Calendar()
            {
                ServiceId = "000054",
                Monday = true,
                Tuesday = true,
                Wednesday = true,
                Thursday = true,
                Friday = true,
                Saturday = true,
                Sunday = true,
                StartDate = new DateTime(2020,01,01),
                EndDate = new DateTime(2020,12,31)
            });
            
            // route_id,service_id,trip_id,trip_headsign,trip_short_name,direction_id,block_id,shape_id,trip_type
            // 32,000054,88____:049::8814258:8814001:10:652:20200208,Bruxelles-Midi,11105,,709,,1
            feed.Trips.Add(new Trip()
            {
                Id = "88____:049::8814258:8814001:10:652:20200208",
                RouteId = "32",
                ServiceId = "000054",
                Headsign = "Bruxelles-Midi",
                ShortName = "11105",
                Direction = null,
                BlockId = "709",
                ShapeId = null
            });
            
            // trip_id,arrival_time,departure_time,stop_id,stop_sequence,stop_headsign,pickup_type,drop_off_type,shape_dist_traveled
            // 88____:049::8814258:8814001:10:652:20200208,06:46:00,06:46:00,8814118,9,,1,1,
            // 88____:049::8814258:8814001:10:652:20200208,06:52:00,06:52:00,8814001,10,,1,0,
            feed.StopTimes.Add(new StopTime()
            {
                TripId= "88____:049::8814258:8814001:10:652:20200208",
                ArrivalTime = new TimeOfDay()
                {
                    Hours = 6,
                    Minutes = 46,
                    Seconds = 0
                },
                DepartureTime = new TimeOfDay()
                {
                    Hours = 6,
                    Minutes = 46,
                    Seconds = 0
                },
                StopId = "8814118",
                StopSequence = 9,
                StopHeadsign = null,
                PickupType = PickupType.Regular,
                DropOffType = DropOffType.Regular,
                ShapeDistTravelled = null
            });
            feed.StopTimes.Add(new StopTime()
            {
                TripId= "88____:049::8814258:8814001:10:652:20200208",
                ArrivalTime = new TimeOfDay()
                {
                    Hours = 6,
                    Minutes = 52,
                    Seconds = 0
                },
                DepartureTime = new TimeOfDay()
                {
                    Hours = 6,
                    Minutes = 52,
                    Seconds = 0
                },
                StopId = "8814001",
                StopSequence = 10,
                StopHeadsign = null,
                PickupType = PickupType.Regular,
                DropOffType = DropOffType.NoPickup,
                ShapeDistTravelled = null
            });

            var transitDb = new TransitDb(0);
            transitDb.LoadGTFS(feed, 
                new DateTime(2020,01,01), new DateTime(2020,01,01));

            Assert.Single(transitDb.Latest.ConnectionsDb);
        }
    }
}