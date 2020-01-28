using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

    internal class Gtfs2Tdb
    {
        private readonly FeedData _f;
        private readonly Dictionary<string, StopId> _gtfsId2TdbId;
        private readonly bool _addEmptyTrips;
        private readonly bool _addUnusedStops;
        private readonly Dictionary<string, Stop> _stops;

        private bool _stopTimesSorted = false;

        public Gtfs2Tdb(FeedData f, bool addEmptyTrips = false, bool addUnusedStops = false)
        {
            _f = f;
            _addEmptyTrips = addEmptyTrips;
            _addUnusedStops = addUnusedStops;
            
            _gtfsId2TdbId = new Dictionary<string, StopId>();
            _stops = new Dictionary<string, Stop>();
        }

        private void AddService(TransitDbWriter writer, string serviceId, DateTime day, DateTime startDate,
            DateTime endDate)
        {
            // We know that 'service' drives today
            // Let's generate the connections and trip for that
            // Note that our 'trip' is slightly different from the gtfs trip:
            // our trip has a specific time and date, whereas a gtfs trip drives at a specific time, over multiple dates

            if (!_f.ServiceIdToTrip.TryGetValue(serviceId, out var gtfsTrips))
            {
                // Seems like there is no trip associated with this service
                // Print an error message and go on

                Log.Error($"No trip found for serviceId {serviceId} found.");

                return;
            }
            
            Log.Information($"Found {gtfsTrips.Count} trips for service {serviceId}");

            // gtfsTrips are the trips we need to add    
            foreach (var gtfsTrip in gtfsTrips) // the trips for this given service
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

            if (_stopTimesSorted)
            {
                // make sure the stop times are sorted by tripid/sequence.
                
            }

            if (string.IsNullOrEmpty(gtfsTrip.BlockId))
            {
                //Log.Warning($"No BlockId given - using tripId {gtfsTrip.Id} instead");
                gtfsTrip.BlockId = gtfsTrip.Id;
            }

            var tripGlobalId = $"{_f.IdentifierPrefix}trip/{gtfsTrip.BlockId}/{day:yyyyMMdd}";

            Transit.Data.Core.Trip vehicleTrip;
            if (writer.TripsDb.TryGet(tripGlobalId, out var existingTrip))
            {
                existingTrip.TryGetAttribute("headsign", out var existingHeadsign);
                existingHeadsign = existingHeadsign ?? "";
                existingTrip.TryGetAttribute("shapeid", out var existingShapeId);
                existingShapeId = existingShapeId ?? "";
                existingTrip.TryGetAttribute("shortName", out var existingShortName);
                existingShortName = existingShortName ?? "";

                // We merge all values with ";"
                // Note: even when no information is given, we add a ";" in order to be able to match them afterwards
                vehicleTrip =
                    new Transit.Data.Core.Trip(tripGlobalId,
                        new Dictionary<string, string>
                        {
                            {"headsign", existingHeadsign + ";" + gtfsTrip.Headsign},
                            {"shapeid", existingShapeId + ";" + gtfsTrip.ShapeId},
                            {"shortname", existingShortName + ";" + gtfsTrip.ShortName}
                        });
            }
            else
            {
                vehicleTrip =
                    new Transit.Data.Core.Trip(tripGlobalId,
                        new Dictionary<string, string>
                        {
                            {"headsign", gtfsTrip.Headsign},
                            {"blockid", gtfsTrip.BlockId},
                            {"shapeid", gtfsTrip.ShapeId},
                            {"shortname", gtfsTrip.ShortName}
                        });
            }

            // add the trip here if empty trips are ok.
            TripId? vehicleTripId = null;
            if (_addEmptyTrips)
            {
                vehicleTripId = writer.AddOrUpdateTrip(vehicleTrip);
            }

            /*
                 Now that we know all about the trip, we have to figure out where and when the train drives
                 For this, we use stopstimes.txt
                 */
            var stopTimes = _f.Feed.StopTimes.GetForTrip(gtfsTrip.Id).OrderBy(stopTime => stopTime.StopSequence)
                .ToList();

            for (var i = 1; i < stopTimes.Count; i++)
            {
                var departure = stopTimes[i - 1];
                var arrival = stopTimes[i];
                var departureTime =
                    day.AddSeconds(departure.DepartureTime.Value.TotalSeconds);
                if (!(startDate <= departureTime && departureTime < endDate))
                {
                    // Departure time out of range
                    continue;
                }


                var travelTime = arrival.ArrivalTime.Value.TotalSeconds - departure.DepartureTime.Value.TotalSeconds;

                if (!_gtfsId2TdbId.TryGetValue(departure.StopId, out var departureStopId))
                {
                    // stop doesn't exist yet, load it.
                    if (!_stops.TryGetValue(departure.StopId, out var departureStop))
                    {
                        Log.Warning($"A connection was found with a non-existing departure stop id: {departure.StopId}");
                        continue;
                    }
                    departureStopId = writer.AddOrUpdateStop(departureStop);
                    _gtfsId2TdbId.Add(departure.StopId, departureStopId);
                }

                if (!_gtfsId2TdbId.TryGetValue(arrival.StopId, out var arrivalStopId))
                {
                    // stop doesn't exist yet, load it.
                    if (!_stops.TryGetValue(arrival.StopId, out var arrivalStop))
                    {
                        Log.Warning($"A connection was found with a non-existing arrival stop id: {arrival.StopId}");
                        continue;
                    }
                    arrivalStopId = writer.AddOrUpdateStop(arrivalStop);
                    _gtfsId2TdbId.Add(arrival.StopId, arrivalStopId);
                }

                var mode = Connection.CreateMode(
                    departure.PickupType == PickupType.Regular,
                    arrival.DropOffType == DropOffType.Regular,
                    false // again: static data, not the messy realworld
                );

                // add trip here, now that we sure we have a connection.
                if (vehicleTripId == null)
                {
                    vehicleTripId = writer.AddOrUpdateTrip(vehicleTrip);
                }

                var connection = new Connection(
                    $"{_f.IdentifierPrefix}connection/{gtfsTrip.Id}/{day:yyyyMMdd}/{i}",
                    departureStopId,
                    arrivalStopId,
                    DateTimeExtensions.ToUnixTime(departureTime.ConvertToUtcFrom(_f.TimeZone)),
                    (ushort) travelTime,
                    mode,
                    vehicleTripId.Value);

                writer.AddOrUpdateConnection(connection);
                
                if (writer.ConnectionsDb.Count % 10000 == 0) Log.Verbose($"Added {writer.ConnectionsDb.Count} connections with " +
                                                                         $"{writer.StopsDb.Count} stops and {writer.TripsDb.Count} trips.");
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

            day = day.Date;

            var services = _f.ServicesForDay(day).ToList();
            Log.Information($"Adding {services.Count} services for day {day:yyyy-MM-dd}");
            foreach (var service in services)
            {
                AddService(writer, service, day, startDate, endDate);
            }
        }

        internal void AddStops(TransitDbWriter writer)
        {
            foreach (var stop in _f.Feed.Stops.Get())
            {
                AddStop(writer, stop);
            }
        }

        private void AddStop(TransitDbWriter writer, global::GTFS.Entities.Stop stop)
        {
            // build the id.
            var id = stop.Url;
            if (string.IsNullOrEmpty(id))
            {
                id = _f.IdentifierPrefix + "stop/" + stop.Id;
            }

            // collect attributes.
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
            if (writer.TryGetAttribute("languagecode", out var languageCode)
                && !string.IsNullOrEmpty(languageCode))
            {
                attributes["name:" + languageCode] = stop.Name;
            }

            // add translated names.
            if (!string.IsNullOrEmpty(stop.Name))
            {
                if (_f.Translations.TryGetValue(stop.Name, out var translated))
                {
                    foreach (var (lng, term) in translated)
                    {
                        attributes["name:" + lng] = term;
                    }
                }
            }

            // add stop and keep id.
            if (_addUnusedStops)
            { // add stop here.
                var stopId = writer.AddOrUpdateStop(
                    new Stop(id, (stop.Longitude, stop.Latitude), attributes));
                _gtfsId2TdbId.Add(stop.Id, stopId);
            }
            else
            { // keep stop for later.
                _stops[stop.Id] = new Stop(id, (stop.Longitude, stop.Latitude), attributes);
            }
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
        internal void AddDataBetween(TransitDbWriter writer, DateTime startdate, DateTime enddate)
        {
            var agencies = _f.Feed.Agencies.ToList();
            if (agencies.Count > 1)
            {
                throw new ArgumentException(
                    "This GTFS contains data on multiple operators, this is not supported at this moment");
            }

            var agency = agencies[0];

            writer.GlobalId = agency.URL;
            writer.AttributesWritable["email"] = agency.Email;
            writer.AttributesWritable["id"] = agency.Id;
            writer.AttributesWritable["name"] = agency.Name;
            writer.AttributesWritable["phone"] = agency.Phone;
            writer.AttributesWritable["timezone"] = agency.Timezone;
            writer.AttributesWritable["languagecode"] = agency.LanguageCode;
            writer.AttributesWritable["url"] = agency.URL;
            writer.AttributesWritable["website"] = agency.URL;
            writer.AttributesWritable["charge:url"] = agency.FareURL;

            // add stops.
            AddStops(writer);
            Log.Information($"Added {_gtfsId2TdbId.Count} stop locations");

            // First things first - lets convert everything to the timezone specified by the GTFS
            startdate = startdate.ConvertTo(_f.TimeZone);
            enddate = enddate.ConvertTo(_f.TimeZone);


            var day = startdate.Date;
            var end = enddate.Date;
            while (day <= end)
            {
                AddDay(writer, day, startdate, enddate);

                day = day.AddDays(1);
            }
        }
    }
}