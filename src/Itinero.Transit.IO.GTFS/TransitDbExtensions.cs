using System;
using System.Collections.Generic;
using System.Linq;
using GTFS;
using GTFS.Entities;
using GTFS.Entities.Enumerations;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Logging;
using Itinero.Transit.Utils;
using Stop = GTFS.Entities.Stop;
using Trip = GTFS.Entities.Trip;

namespace Itinero.Transit.IO.GTFS
{
    /// <summary>
    /// Contains extension methods for the transit db.
    /// </summary>
    public static class TransitDbExtensions
    {
        /// <summary>
        /// Loads a GTFS into the given transit db.
        /// </summary>
        /// <param name="transitDb">The transit db.</param>
        /// <param name="path">The path to the archive containing the GTFS data.</param>
        /// <param name="startDate">The start date/time of the data to load.</param>
        /// <param name="endDate">The end date/time of the data to load</param>
        /// <param name="settings">The settings.</param>
        public static void LoadGTFS(this TransitDb transitDb, string path, DateTime startDate, DateTime endDate,
            GTFSLoadSettings settings = null)
        {
            if (startDate.Date != startDate) throw new ArgumentException($"{nameof(startDate)} should only contain a date component.", $"{nameof(startDate)}");
            if (endDate.Date != endDate) throw new ArgumentException($"{nameof(startDate)} should only contain a date component.", $"{nameof(startDate)}");
            
            // read GTFS feed.
            IGTFSFeed feed = null;
            try
            {
                feed = new GTFSReader<GTFSFeed>().Read(path);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to read GTFS feed: {e}");
                throw;
            }
            
            // load feed.
            transitDb.LoadGTFS(feed, startDate, endDate, settings);
        }

        /// <summary>
        /// Loads a GTFS into the given transit db.
        /// </summary>
        /// <param name="transitDb">The transit db.</param>
        /// <param name="feed">The path to the archive containing the GTFS data.</param>
        /// <param name="startDate">The start date/time of the data to load.</param>
        /// <param name="endDate">The end date/time of the data to load.</param>
        /// <param name="settings">The settings.</param>
        public static void LoadGTFS(this TransitDb transitDb, IGTFSFeed feed, DateTime startDate, DateTime endDate,
            GTFSLoadSettings settings = null)
        {
            if (startDate.Date != startDate)
                throw new ArgumentException($"{nameof(startDate)} should only contain a date component.",
                    $"{nameof(startDate)}");
            if (endDate.Date != endDate)
                throw new ArgumentException($"{nameof(endDate)} should only contain a date component.",
                    $"{nameof(endDate)}");
            settings ??= new GTFSLoadSettings();
            
            // get the transit db writer.
            var writer = transitDb.GetWriter();
            
            // add agency first.
            var agencyMap = writer.AddAgencies(feed);
            
            // get the id prefix/timezone.
            var idPrefix = feed.IdentifierPrefix();
            var timeZone = feed.GetTimeZoneInfo();
            
            // convert to proper timezome.
            startDate = startDate.ToUniversalTime().ConvertTo(timeZone).Date;
            endDate = endDate.ToUniversalTime().ConvertTo(timeZone).Date;

            try
            {
                // get the stops and index them or add them.
                var stops = feed.GetStops(idPrefix: idPrefix);
                var stopIndex = new Dictionary<string, (Itinero.Transit.Data.Core.Stop stop, StopId? stopDbId)>();
                foreach (var (stopId, stop) in stops)
                {
                    if (settings.AddUnusedStops)
                    {
                        var stopDbId = writer.AddOrUpdateStop(stop);
                        stopIndex[stopId] = (stop, stopDbId);
                    }
                    else
                    {
                        stopIndex[stopId] = (stop, null);
                    }
                }
                
                // get the routes index.
                var routes = feed.GetRoutes();

                // check and build service schedules.
                var services = feed.GetDatePatterns();

                // sort items in feed to ease loading the data.
                // sort by trip id.
                using var trips = feed.Trips.OrderBy(x => x.Id).ThenBy(x => x.ServiceId).GetEnumerator();
                // sort by trip id / sequence.
                var stopTimes = feed.StopTimes.OrderBy(x => x.TripId).ThenBy(x => x.StopSequence).GetEnumerator();
                var stopTime = stopTimes.MoveNext() ? stopTimes.Current : null;

                Log.Verbose($"Parsing trips...");
                var days = (int) (endDate - startDate).TotalDays + 1;
                var dbTrips = new (string tripId, TripId? tripDbId)[days];
                while (trips.MoveNext())
                {
                    var trip = trips.Current;
                    if (trip == null) continue;
                    
                    // get service if any.
                    if (!services.TryGetValue(trip.ServiceId, out var service)) continue;
                    
                    // get route if any.
                    var operatorId = OperatorId.Invalid;
                    if (!routes.TryGetValue(trip.RouteId, out var route))
                    {
                        Log.Warning($"Route {trip.RouteId} not found for trip {trip.Id}: No route details will be available on this trip.");
                    }
                    else
                    {
                        if (!agencyMap.TryGetValue(route.AgencyId, out operatorId))
                        {
                            Log.Warning($"Route {trip.RouteId} has an unknown agency: {route.AgencyId}");
                        }
                    }
                    
                    // TODO: check if this is ok to continue with.
                    // build the trips for each day one.
                    // add here if all trips should be added.
                    for (var d = 0; d < days; d++)
                    {
                        var day = startDate.AddDays(d);
                        if (!service.IsActiveOn(day))
                        {
                            dbTrips[d] = (null, null);
                        }
                        else
                        {
                            if (settings.AddUnusedTrips)
                            {
                                var tripDb = trip.ToItineroTrip(day, idPrefix: idPrefix, route: route, op: operatorId);
                                dbTrips[d] = (trip.ToItineroTripId(day, idPrefix: idPrefix), 
                                    writer.AddOrUpdateTrip(tripDb));
                            }
                            else
                            {
                                dbTrips[d] = (trip.ToItineroTripId(day, idPrefix: idPrefix), null);
                            }
                        }
                    }

                    // skip stoptimes with non-existent trips.
                    while (stopTime != null &&
                           string.Compare(stopTime.TripId, trip.Id, StringComparison.CurrentCulture) < 0)
                    {
                        stopTime = stopTimes.MoveNext() ? stopTimes.Current : null;
                    }

                    // loop over the trips stop times.
                    StopTime previous = null;
                    while (stopTime != null && stopTime.TripId == trip.Id)
                    {
                        //Log.Verbose($"StopTime - [{stopTime.TripId}|{stopTime.StopSequence}]: {stopTime}");

                        if (previous != null)
                        {
                            Itinero.Transit.Data.Core.StopId? fromStopDbId = null;
                            Itinero.Transit.Data.Core.StopId? toStopDbId = null;
                            for (var d = 0; d < days; d++)
                            {
                                if (previous.DepartureTime == null)
                                {
                                    Log.Warning(
                                        $"A stoptime object was found without an departure time: {previous}");
                                    continue;
                                }
                                if (stopTime.ArrivalTime == null)
                                {
                                    Log.Warning(
                                        $"A stoptime object was found without an arrival time: {stopTime}");
                                    continue;
                                }
                                
                                var day = startDate.AddDays(d);
                                
                                // make sure the trip is there.
                                var tripDbPair = dbTrips[d];
                                if (tripDbPair.tripId == null) continue;
                                if (!tripDbPair.tripDbId.HasValue)
                                {
                                    tripDbPair = (tripDbPair.tripId,
                                        writer.AddOrUpdateTrip(trip.ToItineroTrip(day, idPrefix: idPrefix, route: route, op: operatorId)));
                                    dbTrips[d] = tripDbPair;
                                }
                                var departureTime =
                                    day.AddSeconds(previous.DepartureTime.Value.TotalSeconds);

                                // make sure the stops are there.
                                if (fromStopDbId == null)
                                {
                                    if (!stopIndex.TryGetValue(previous.StopId, out var fromStopData))
                                    {
                                        Log.Warning(
                                            $"A stoptime object was found with a stop that doesn't exist: {previous}");
                                        break;
                                    }

                                    if (!fromStopData.stopDbId.HasValue)
                                    {
                                        fromStopDbId = writer.AddOrUpdateStop(fromStopData.stop);
                                        stopIndex[previous.StopId] = (fromStopData.stop, fromStopDbId);
                                    }
                                    else
                                    {
                                        fromStopDbId = fromStopData.stopDbId.Value;
                                    }

                                    if (fromStopData.stop.TryUpdateRouteOnStop(route, out var newStop))
                                    {
                                        fromStopData.stop = newStop;
                                        stopIndex[previous.StopId] = (fromStopData.stop, fromStopDbId);
                                        fromStopDbId = writer.AddOrUpdateStop(fromStopData.stop);
                                    }
                                }

                                if (toStopDbId == null)
                                {
                                    if (!stopIndex.TryGetValue(stopTime.StopId, out var toStopData))
                                    {
                                        Log.Warning(
                                            $"A stoptime object was found with a stop that doesn't exist: {stopTime}");
                                        break;
                                    }

                                    if (!toStopData.stopDbId.HasValue)
                                    {
                                        toStopDbId = writer.AddOrUpdateStop(toStopData.stop);
                                        stopIndex[stopTime.StopId] = (toStopData.stop, toStopDbId);
                                    }
                                    else
                                    {
                                        toStopDbId = toStopData.stopDbId.Value;
                                    }

                                    if (toStopData.stop.TryUpdateRouteOnStop(route, out var newStop))
                                    {
                                        toStopData.stop = newStop;
                                        stopIndex[previous.StopId] = (toStopData.stop, toStopDbId);
                                        toStopDbId = writer.AddOrUpdateStop(toStopData.stop);
                                    }
                                }

                                // create mode.
                                var mode = Connection.CreateMode(
                                previous.PickupType == PickupType.Regular,
                                stopTime.DropOffType == DropOffType.Regular,
                                false // again: static data, not the messy realworld
                                 );
                                
                                var travelTime = stopTime.ArrivalTime.Value.TotalSeconds - previous.DepartureTime.Value.TotalSeconds;
                                var connection = new Connection(
                                    $"{idPrefix}connection/{trip.Id}/{day:yyyyMMdd}/{previous.StopSequence}",
                                    fromStopDbId.Value,
                                    toStopDbId.Value,
                                    DateTimeExtensions.ToUnixTime(departureTime.ConvertToUtcFrom(timeZone)),
                                    (ushort) travelTime,
                                    mode,
                                    tripDbPair.tripDbId.Value);

                                writer.AddOrUpdateConnection(connection);

                                if (writer.ConnectionsDb.Count % 100000 == 0)
                                    Log.Verbose($"Added {writer.ConnectionsDb.Count} connections with " +
                                                $"{writer.StopsDb.Count} stops and {writer.TripsDb.Count} trips.");
                            }
                        }

                        previous = stopTime;
                        stopTime = stopTimes.MoveNext() ? stopTimes.Current : null;
                    }
                }
            }
            finally
            {
                writer.Close();
            }
        }

        internal static bool TryUpdateRouteOnStop(this Itinero.Transit.Data.Core.Stop stop, Route route, out Transit.Data.Core.Stop newStop)
        {
            newStop = null;
            if (route == null) return false;
            
            var newRouteType = ((int) route.Type).ToString();
            
            var i = 0;
            while (stop.Attributes.TryGetValue($"route_type_{i:00000}", out var routeType))
            {
                if (routeType == newRouteType)
                {
                    newStop = stop;
                    return false;
                }

                i++;
            }

            var attributes = new Dictionary<string, string>();
            foreach (var attribute in stop.Attributes)
            {
                attributes[attribute.Key] = attribute.Value;
            }
            attributes[$"route_type_{i:00000}"] = newRouteType;

            newStop = new Transit.Data.Core.Stop(stop.GlobalId, (stop.Longitude, stop.Latitude), attributes);
            return true;
        }

        internal static string ToItineroTripId(this Trip gtfsTrip, DateTime day, string idPrefix = null)
        {
            idPrefix ??= string.Empty;
            if (string.IsNullOrEmpty(gtfsTrip.BlockId))
            {
                return $"{idPrefix}trip/{gtfsTrip.Id}/{day:yyyyMMdd}";
            }

            return $"{idPrefix}trip/{gtfsTrip.BlockId}/{day:yyyyMMdd}";
        }

        internal static Itinero.Transit.Data.Core.Trip ToItineroTrip(this Trip gtfsTrip, DateTime day, string idPrefix = null,
            Route route = null, OperatorId? op = null)
        {
            if (day.Date != day) 
                throw new ArgumentException($"{nameof(day)} should only contain a date component.",
                    $"{nameof(day)}");
            idPrefix ??= string.Empty;
            op ??= OperatorId.Invalid;

            var attributes = new Dictionary<string, string>
            {
                {"headsign", gtfsTrip.Headsign},
                {"blockid", gtfsTrip.BlockId},
                {"shapeid", gtfsTrip.ShapeId},
                {"shortname", gtfsTrip.ShortName}
            };

            if (route != null)
            {
                attributes["route_description"] = route.Description;
                attributes["route_url"] = route.Url;
                attributes["route_type"] = ((int) route.Type).ToString();
                attributes["route_longname"] = route.LongName;
                attributes["route_shortname"] = route.ShortName;
                attributes["route_color"] = route.Color.ToHexColorString();
                attributes["route_textcolor"] = route.TextColor.ToHexColorString();
            }
            
            return new Transit.Data.Core.Trip(gtfsTrip.ToItineroTripId(day, idPrefix), op.Value,
                attributes);
        }

        internal static Dictionary<string, OperatorId> AddAgencies(this TransitDbWriter writer, IGTFSFeed feed)
        {
            if (feed == null) throw new ArgumentNullException(nameof(feed));
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            var useUrlAsGlobalId = true;
            var agencyUrls = new HashSet<string>();
            foreach (var agency in feed.Agencies)
            {
                if (string.IsNullOrEmpty(agency.URL))
                {
                    useUrlAsGlobalId = false;
                    break;
                }

                if (agencyUrls.Contains(agency.URL))
                {
                    useUrlAsGlobalId = false;
                    break;
                }
                agencyUrls.Add(agency.URL);
            }
            
            var agencyMap = new Dictionary<string, OperatorId>();
            foreach (var agency in feed.Agencies)
            {
                var globalId = agency.Id;
                if (useUrlAsGlobalId)
                {
                    globalId = agency.URL;
                }

                var attributes = new Dictionary<string, string>
                {
                    {"email", agency.Email},
                    {"id", agency.Id},
                    {"name", agency.Name},
                    {"phone", agency.Phone},
                    {"timezone", agency.Timezone},
                    {"languagecode", agency.LanguageCode},
                    {"url", agency.URL},
                    {"website", agency.URL},
                    {"charge:url", agency.FareURL}
                };

                var op = new Operator(globalId, attributes);

                var opId = writer.AddOrUpdateOperator(op);

                agencyMap[agency.Id] = opId;
            }

            return agencyMap;
        }
    }
}