using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using GTFS;
using GTFS.Entities;
using GTFS.Entities.Enumerations;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Logging;
using Itinero.Transit.Utils;
using Stop = Itinero.Transit.Data.Core.Stop;
using Trip = GTFS.Entities.Trip;
// ReSharper disable PossibleInvalidOperationException

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]

namespace Itinero.Transit.IO.GTFS.Data
{
    /*
     * How GTFS works:
     *
     * stops.txt contains metadata about the stops (id --> stopdata), pretty straightforward
     *
     * stop_times.txt contains the stop times of a typical trip, e.g:
     * trip_1    stop_X  10:00 (boarding)   10:03 (departure)     #1 (thus: first stop in trip_1)
     * trip_1    stop_Y  10:10 (arrival)    10:23 (departure)     #2
     * trip_1    stop_Z  10:20 (arrival)    ----- (terminus)      #3
     * trip_2    stop_A  10:05 (boarding)   10:08 (departure)     #1 (thus: first stop in trip_2)
     * trip_2    stop_B  10:15 (arrival)    10:28 (departure)     #2
     * trip_2    stop_C  10:25 (arrival)    ----- (terminus))     #3
     *
     *
     * Trips.txt gives metadata about the trip, e.g.
     *
     * trip_1    headsign:"Towards Z"    service_weekday
     *
     * The service tells us which exact dates that trip_1 drives. (Note: this implies there is at most ONE trip_1 per day)
     * 
     * service_weekday    startdate:2020-01-01    enddate: 2020-03-01 on:monday,tuesday,wednesday,thursday,friday
     *
     * This service implies that:
     * - every date between the start- and enddate should be enumerated
     * - If it matches the weekday, then the trip should be generated:, e.g. monday 2020-01-07, trip_1 will travel
     * - This implies that the following TWO connections are known:
     *     Stop_X, 2020-01-07T10:03 -> Stop_Y, 2020-01-07T10:10
     *     Stop_Y, 2020-01-07T10:13 -> Stop_Z, 2020-01-07T10:20 (where the train stops

     */

    public class Gtfs2Tdb
    {
        private readonly string _path;


        private GTFSFeed _feed;

        private GTFSFeed Feed
        {
            get
            {
                if (_feed == null)
                {
                    _feed = new GTFSReader<GTFSFeed>().Read(_path);
                }

                return _feed;
            }
        }


        private string _prefix;

        /// <summary>
        /// Get the identifier-prefix for this GTFS feed.
        /// The identifier-prefix starts with the agencies website ('http://belgiantrain.be/') and has a trailing slash.
        ///
        /// If the GTFS feed contains multiple agencies, an error is thrown
        /// </summary>
        /// <returns></returns>
        internal string IdentifierPrefix
        {
            get
            {
                if (_prefix != null)
                {
                    return _prefix;
                }

                var urls = AgencyUrls();
                if (urls.Count > 1)
                {
                    throw new ArgumentException("The GTFS archive " + _path + " contains data on multiple agencies");
                }

                var prefix = urls[0];
                if (!prefix.EndsWith("/"))
                {
                    prefix += "/";
                }

                return _prefix = prefix;
            }
        }


        private Dictionary<string, List<Trip>> _serviceIdToTrip;
        private Dictionary<string, StopId> _gtfsId2TdbId;

        private Dictionary<string, List<Trip>> ServiceIdToTrip
        {
            get
            {
                if (_serviceIdToTrip == null)
                {
                    _serviceIdToTrip = new Dictionary<string, List<Trip>>();

                    foreach (var gtfstrip in Feed.Trips)
                    {
                        if (!_serviceIdToTrip.ContainsKey(gtfstrip.ServiceId))
                        {
                            _serviceIdToTrip[gtfstrip.ServiceId] = new List<Trip>();
                        }

                        _serviceIdToTrip[gtfstrip.ServiceId].Add(gtfstrip);
                    }
                }


                return _serviceIdToTrip;
            }
        }


        public Gtfs2Tdb(string path)
        {
            _path = path;
        }


        private void AddService(TransitDbWriter writer, Calendar service, DateTime day, DateTime startDate, DateTime endDate)
        {
            // We know that 'service' drives today
            // Let's generate the connections and trip for that
            // Note that our 'trip' is slightly different from the gtfs trip:
            // our trip has a specific time and date, whereas a gtfs trip drives at a specific time, over multiple dates

            if (!ServiceIdToTrip.TryGetValue(service.ServiceId, out var gtfsTrips))
            {
                // Seems like there is no trip associated with this service
                // Print an error message and go on
                
                Log.Error($"No trip with id {service.ServiceId} found.");
                
                return;
            }
            // gtfsTrips are the trips we need to add    
            foreach (var gtfsTrip in gtfsTrips)
            {
                AddCompleteTrip(writer, gtfsTrip, day, startDate, endDate);
            }
        }

        private void AddCompleteTrip(TransitDbWriter writer, Trip gtfsTrip, DateTime day, DateTime startDate,
            DateTime endDate)
        {
            /* Note that we use 'blockId' to generate the tripID
             A block identifies the vehicle throughout the day
             An example is the line Brugge -> ....... -> Kortrijk -> ....... -> Gent
             (The -> ..... -> represents many small stops in between)
             It is advertised as two distinct trips: the part 'Brugge' -> ..... -> 'Kortrijk' and 'Kortrijk' -> .... -> 'Gent'
             However, in practice, this is one continuous trip, and the traveller can stay seated on it (e.g. to depart in Brugge and reach one of the small stops between Kortrijk and Ghent)
            
             Another advantage is that, if two trains join at C and split again at D:
             
             A                x
               \            /
                 C =====> D
               /            \
             B                Y
             
             (Train 1: A -> C -> D -> X)
             (Train 2: B -> C -> D -> Y)
             
             A passenger going from A to Y still has to "transfer" (he has to get out, and get in to the other part of the train)
             
             The blockID neatly captures this and will force a transfer onto the passenger.
             
            */

            /*
                  A disadvantage is that we loose the neat, predefined trips, e.g. the journey above 
                  Brugge -> .... -> Kortrijk -> .... -> Ghent is not individually traceable anymore.
                  We solve this by adding some extra tagging onto the connections and a secondary trip.
                  E.g. information such as 'Headsign', 'Route', ... are tacked onto a secondary trip element which contains the actual metadat
                  Note that the identifier of this trip-meta is added to the connections as well.
                  The GlobalId of the connection does use the tripID as well, in order to make the metatrip-id recoverable
                 */

            var vehicleTrip =
                new Transit.Data.Core.Trip($"{IdentifierPrefix}trip-by-vehicle/{gtfsTrip.BlockId}/{day:yyyyMMdd}");
            var vehicleTripId = writer.AddOrUpdateTrip(vehicleTrip);

            var tripMeta = new Transit.Data.Core.Trip($"{IdentifierPrefix}trip-meta/{gtfsTrip.Id}/{day:yyyyMMdd}",
                new Dictionary<string, string>
                {
                    {"headsign", gtfsTrip.Headsign},
                    {"blockid", gtfsTrip.BlockId},
                    {"vehicle-trip", vehicleTrip.GlobalId},
                    {"shapeid", gtfsTrip.ShapeId},
                    {"shortname", gtfsTrip.ShortName}
                });
            writer.AddOrUpdateTrip(tripMeta);

            /*
                 Now that we know all about the trip, we have to figure out where and when the train drives
                 For this, we use stopstimes.txt
                 */
            var stopTimes = Feed.StopTimes.GetForTrip(gtfsTrip.Id).OrderBy(stopTime => stopTime.StopSequence).ToList();

            for (var i = 1; i < stopTimes.Count; i++)
            {
                var departure = stopTimes[i - 1];
                var arrival = stopTimes[i];
                var departureTime = day.AddSeconds(departure.DepartureTime.Value.TotalSeconds);
                if (!(startDate <= departureTime && departureTime < endDate))
                {
                    // Departure time out of range
                    continue;
                }
                
                
                var travelTime = arrival.ArrivalTime.Value.TotalSeconds - departure.DepartureTime.Value.TotalSeconds;
                
                
                
                var departureStop = _gtfsId2TdbId[departure.StopId];
                var arrivalStop = _gtfsId2TdbId[arrival.StopId];


                var mode = Connection.CreateMode(
                    departure.PickupType == PickupType.Regular,
                    arrival.DropOffType == DropOffType.Regular,
                    false // again: static data, not the messy realworld
                );
                
                var connection = new Connection(
                    $"{IdentifierPrefix}connection/{gtfsTrip.Id}/{day:yyyyMMdd}/{i}",
                    departureStop,
                    arrivalStop,
                    DateTimeExtensions.ToUnixTime(departureTime),
                    (ushort) travelTime,
                    0, // We are working with static data, the perfect world without delays
                    0,
                    mode,
                    vehicleTripId);

                writer.AddOrUpdateConnection(connection);
            }
        }

        /// <summary>
        /// Adds all the trips and connections that are driving on the given day.
        /// Connections departing (strictly) before startDate or departing after or on endDate are not added
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="day"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <exception cref="Exception"></exception>

        internal void AddDay(TransitDbWriter writer, DateTime day, DateTime startDate, DateTime endDate)
        {
            if (_gtfsId2TdbId == null)
            {
                throw new Exception("Please, run AddLocations before trying to add the time schedule");
            }

            // Figure out what services (don't) drive today
            // For this, we ask both the exceptions and the regular schedule
            day = day.Date;

            var exceptionallyGoesToday = new HashSet<string>();
            var cancelledToday = new HashSet<string>();

            foreach (var exception in Feed.CalendarDates)
            {
                if (!exception.Date.Equals(day)) continue;

                if (exception.ExceptionType == ExceptionType.Added)
                {
                    exceptionallyGoesToday.Add(exception.ServiceId);
                }
                else
                {
                    cancelledToday.Add(exception.ServiceId);
                }
            }

            foreach (var service in Feed.Calendars)
            {
                if (cancelledToday.Contains(service.ServiceId))
                {
                    continue;
                }

                var goesToday =
                    service[day.DayOfWeek] &&
                    service.StartDate <= day &&
                    day <= service
                        .EndDate; // end date is included in the interval: https://developers.google.com/transit/gtfs/reference#calendartxt
                if (goesToday || exceptionallyGoesToday.Contains(service.ServiceId))
                {
                    // This service drives today. Generate the connections
                    AddService(writer, service, day, startDate, endDate);
                }
            }
        }


        private Dictionary<string, List<(string language, string translatedTerm)>>
            GetTranslations()
        {
            var result = new Dictionary<string, List<(string language, string translatedTerm)>>();

            // TODO FIXME
            result["Bruges"] = new List<(string language, string translatedTerm)>
            {
                ("nl", "Brugge"),
                ("fr", "Bruges"),
                ("es", "Brugas"),
                ("en", "Bruges"),
                ("de", "Brügge")
            };
            return result;
        }


        internal Dictionary<string, StopId> AddLocations(TransitDbWriter writer)
        {
            _gtfsId2TdbId = new Dictionary<string, StopId>();

            var translations = GetTranslations();

            foreach (var stop in Feed.Stops.Get())
            {
                var id = stop.Url;
                if (string.IsNullOrEmpty(id))
                {
                    id = IdentifierPrefix + "stop/" + stop.Id;
                }

                var attributes = new Dictionary<string, string>
                {
                    {"name", stop.Name},
                    {"code", stop.Code},
                    {"description", stop.Description},
                    {"parent_station", stop.ParentStation},
                    {"platform", stop.PlatformCode},
                    {"levelid", stop.LevelId},
                    {"wheelchairboarding", stop.WheelchairBoarding},
                    {"zone", stop.Zone},
                };

                if (translations.TryGetValue(stop.Name, out var translated))
                {
                    foreach (var (lng, term) in translated)
                    {
                        attributes.Add("name:" + lng, term);
                    }
                }

                var stopId = writer.AddOrUpdateStop(
                    new Stop(id, (stop.Longitude, stop.Latitude), attributes));
                _gtfsId2TdbId.Add(stop.Id, stopId);
            }

            return _gtfsId2TdbId;
        }

        /// <summary>
        /// Does all the work:
        /// - Adds all locations
        /// - Adds all necessary trips of the given timeperiod
        /// - Adds all connections with departure times between startDate (incl) and enddate (excl)
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="startdate"></param>
        /// <param name="enddate"></param>
        public void AddDataBetween(TransitDbWriter writer, DateTime startdate, DateTime enddate)
        {
            AddLocations(writer);

            var day = startdate.Date;
            var end = enddate.Date;
            while (day <= end)
            {
                AddDay(writer, day, startdate, enddate);

                day = day.AddDays(1);
            }

        }

        internal List<string> AgencyUrls()
        {
            return Feed.Agencies.Get().ToList().Select(agency => agency.URL).ToList();
        }
    }
}