using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.IO.LC.Data;
using Itinero.Transit.IO.LC.Utils;
using Itinero.Transit.Logging;
using Connection = Itinero.Transit.IO.LC.Data.Connection;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests.IO.LC")]

namespace Itinero.Transit.IO.LC
{
    internal static class WriterExtensions
    {
        public static void AddAllLocations(this TransitDbWriter writer,
            LinkedConnectionDataset linkedConnectionDataset)
        {
            writer.AddAllLocations(linkedConnectionDataset.LocationProvider);
        }

        internal static void AddAllLocations(this TransitDbWriter writer, LocationFragment locationsFragment)
        {
            foreach (var location in locationsFragment.Locations)
            {
                writer.AddLocation(location);
            }

            Log.Information(
                $"Importing locations: All {locationsFragment.Locations.Count} locations imported");
        }

        public static (int loaded, int reused) AddAllConnections(this TransitDbWriter writer,
            LinkedConnectionDataset p, DateTime startDate,
            DateTime endDate)
        {
            if (startDate.Kind != DateTimeKind.Utc || endDate.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Please provide all dates in UTC");
            }

            var loaded = 0;
            var reused = 0;
            var cons = p.ConnectionsProvider;
            var loc = p.LocationProvider;
            var (l, r) = writer.AddTimeTableWindow(cons, loc, startDate, endDate);
            loaded += l;
            reused += r;

            return (loaded, reused);
        }


        private static (int loaded, int ofWhichReused) AddTimeTableWindow(this TransitDbWriter writer,
            ConnectionProvider cons, LocationFragment locations,
            DateTime startDate, DateTime endDate)
        {
            var currentTimeTableUri = cons.TimeTableIdFor(startDate);

            var count = 0;
            var reused = 0;
            TimeTable timeTable;
            do
            {
                bool wasChanged;
                (timeTable, wasChanged) = cons.GetTimeTable(currentTimeTableUri);
                count++;


                if (wasChanged)
                {
                    var connectionCount = writer.AddTimeTable(timeTable, locations);
                    Log.Information(
                        $"Imported timetable #{count} with {connectionCount} connections)");
                }
                else
                {
                    reused++;
                }

                currentTimeTableUri = timeTable.NextTable();
            } while (timeTable.EndTime() < endDate);

            Log.Information($"Imported {count} timetable, skipped {reused} timetables (no changes)");
            return (count, reused);
        }

        private static int AddTimeTable(this TransitDbWriter writer, TimeTable tt, LocationFragment locations)
        {
            var count = 0;
            tt.Validate(locations);
            foreach (var connection in tt.Connections())
            {
                writer.AddConnection(connection, locations);
                count++;
            }

            return count;
        }

        private static StopId AddLocation(this TransitDbWriter writer, Location location)
        {
            var globalId = location.Uri;
            var stopId = globalId.ToString();

            var attributes = new Dictionary<string, string> {["name"] = location.Name};
            // ReSharper disable once InvertIf
            if (location.Names != null)
            {
                foreach (var (lang, name) in location.Names)
                {
                    attributes[$"name:{lang}"] = name;
                }
            }

            return writer.AddOrUpdateStop(new Stop(stopId, (location.Lon, location.Lat), attributes));
        }

        private static StopId
            AddStop(this TransitDbWriter writer, LocationFragment profile, Uri stopUri)
        {
            var location = profile.GetCoordinateFor(stopUri);
            if (location == null)
            {
                throw new ArgumentException($"Location {stopUri} not found. Run validation first please!");
            }

            return writer.AddLocation(location);
        }


        private static void AddConnection(this TransitDbWriter writer, Connection connection,
            LocationFragment locations)
        {
            
            var stop1Id = writer.AddStop(locations, connection.DepartureLocation());
            var stop2Id = writer.AddStop(locations, connection.ArrivalLocation());

            var tripId = writer.AddTrip(connection);

            var connectionUri = connection.Uri.ToString();


            ushort mode = 0;
            if (!connection.GetOff)
            {
                mode += 1;
            }

            if (!connection.GetOn)
            {
                mode += 2;
            }

            if (connection.IsCancelled)
            {
                mode += 4;
            }

            writer.AddOrUpdateConnection(
                new Transit.Data.Core.Connection(
                stop1Id, stop2Id, connectionUri,
                connection.DepartureTime(),
                (ushort) (connection.ArrivalTime() - connection.DepartureTime()).TotalSeconds,
                connection.DepartureDelay, connection.ArrivalDelay, tripId, mode));
        }


        private static TripId AddTrip(this TransitDbWriter writer, Connection connection)
        {
            var tripUri = connection.Trip().ToString();

            var attributes = new Dictionary<string, string>
            {
               {"headsign", connection.Direction},
               {"trip", $"{connection.Trip()}"},
               {"route", $"{connection.Route()}"}
            };
            return writer.AddOrUpdateTrip(new Trip(tripUri, attributes));
        }
    }
}