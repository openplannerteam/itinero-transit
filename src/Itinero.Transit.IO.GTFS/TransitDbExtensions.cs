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
            writer.AddAgencies(feed);
            
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
                                var tripDb = trip.ToItineroTrip(day, idPrefix: idPrefix);
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
                                        writer.AddOrUpdateTrip(trip.ToItineroTrip(day, idPrefix: idPrefix)));
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

        internal static string ToItineroTripId(this Trip gtfsTrip, DateTime day, string idPrefix = null)
        {
            idPrefix ??= string.Empty;
            if (string.IsNullOrEmpty(gtfsTrip.BlockId))
            {
                return $"{idPrefix}trip/{gtfsTrip.Id}/{day:yyyyMMdd}";
            }

            return $"{idPrefix}trip/{gtfsTrip.BlockId}/{day:yyyyMMdd}";
        }

        internal static Itinero.Transit.Data.Core.Trip ToItineroTrip(this Trip gtfsTrip, DateTime day, string idPrefix = null)
        {
            if (day.Date != day) 
                throw new ArgumentException($"{nameof(day)} should only contain a date component.",
                    $"{nameof(day)}");
            idPrefix ??= string.Empty;
            
            return new Transit.Data.Core.Trip(gtfsTrip.ToItineroTripId(day, idPrefix),
                new Dictionary<string, string>
                {
                    {"headsign", gtfsTrip.Headsign},
                    {"blockid", gtfsTrip.BlockId},
                    {"shapeid", gtfsTrip.ShapeId},
                    {"shortname", gtfsTrip.ShortName}
                });
        }

        internal static void AddAgencies(this TransitDbWriter writer, IGTFSFeed feed)
        {
            if (feed == null) throw new ArgumentNullException(nameof(feed));
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            
            if (feed.Agencies.Count > 1) throw new NotSupportedException("A feed with more than one agency is currently not supported, filter feed before loading.");

            if (feed.Agencies.Count == 0)
            {
                Log.Warning("Feed has no agencies.");
                return;
            }
            
            var agency = feed.Agencies.Get(0);
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
        }
    }
}