using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.CSA.Connections;
using Itinero.Transit.Logging;
using Attribute = Itinero.Transit.Data.Attributes.Attribute;

namespace Itinero.Transit.IO.LC
{
    /// <summary>
    /// Contains extensions methods related to the transit db.
    /// </summary>
    public static class TransitDbExtensions
    {
        private static (uint tileId, uint localId) AddStop(ILocationProvider profile, Uri stopUri, TransitDb.TransitDbWriter writer)
        {
            var stop1Uri = stopUri;
            var stop1Location = profile.GetCoordinateFor(stop1Uri);
            if (stop1Location == null)
            {
                return (uint.MaxValue, uint.MaxValue);
            }
            
            return writer.AddOrUpdateStop(stop1Uri.ToString(), stop1Location.Lon, stop1Location.Lat,
                new Attribute("name", stop1Location.Name));
        }

        private static uint AddTrip(LinkedConnection connection, TransitDb.TransitDbWriter writer)
        {
            var tripUri = connection.Trip().ToString();

            return writer.AddOrUpdateTrip(tripUri, new Attribute("headsign", connection.Direction),
                new Attribute("trip", connection.Trip().ToString()),
                new Attribute("route", connection.Route().ToString()));
        }

        private static void AddConnection(LinkedConnection connection, Profile profile, TransitDb.TransitDbWriter writer)
        {
            var stop1Id = AddStop(profile, connection.DepartureLocation(), writer);
            var stop2Id = AddStop(profile, connection.ArrivalLocation(), writer);

            if (stop1Id.localId == uint.MaxValue && stop1Id.tileId == uint.MaxValue &&
                stop2Id.localId == uint.MaxValue && stop2Id.tileId == uint.MaxValue)
            {
                return;
            }

            var tripId = AddTrip(connection, writer);

            var connectionUri = connection.Id().ToString();

            writer.AddOrUpdateConnection(stop1Id, stop2Id,connectionUri, connection.DepartureTime(),
                (ushort) (connection.ArrivalTime() - connection.DepartureTime()).TotalSeconds, tripId);
        }

//        /// <summary>
//        /// Loads connections into the connections db and the given stops db from the given profile.
//        /// </summary>
//        /// <param name="profile">The profile.</param>
//        /// <param name="stopsDb">The stops db.</param>
//        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
//        public static void LoadLocations(this StopsDb stopsDb,
//            Profile profile, Action<int, int> locationsLoadedPing = null)
//        {
//            var locations = profile.GetAllLocations();
//            var i = 0;
//            var length = locations.Count();
//            foreach (var l in locations)
//            {
//                stopsDb.Add(l.Uri.ToString(), l.Lon, l.Lat, new Attribute("name", l.Name));
//                i++;
//                // ReSharper disable once InvertIf
//                if (i % 100 == 0)
//                {
//                    Log.Verbose($"Added location {i}/{length}");
//                    locationsLoadedPing?.Invoke(i, length);
//                }
//            }
//
//            locationsLoadedPing?.Invoke(i, length);
//        }

        /// <summary>
        /// Loads connections and stops into the transit db from the given profile.
        /// </summary>
        /// <param name="transitDb">The transit db.</param>
        /// <param name="profile">The profile.</param>
        /// <param name="window">The window, a start time and duration.</param>
        /// <param name="connectionsLoadedPing">Is called every 1000 loaded connections with the connection number, date and percentage</param>
        public static void LoadConnections(this TransitDb transitDb,
            Profile profile, (DateTime start, TimeSpan duration) window,
            Action<LinkedConnection> onEach = null,
            Action<int, DateTime, double> connectionsLoadedPing = null)
        {
            var writer = transitDb.GetWriter();
            
            try
            {
                Log.Information("Building the database... Hang on");

                var connectionCount = 0;
                var currentTimeTableUri = profile.TimeTableIdFor(window.start);

                var endTime = window.start + window.duration;

                ITimeTable timeTable;
                do
                {
                    timeTable = profile.GetTimeTable(currentTimeTableUri);
                    foreach (var connection in timeTable.Connections())
                    {
                        if (connection.DepartureTime() < window.start)
                        {
                            continue;
                        }

                        AddConnection(connection, profile, writer);
                        onEach?.Invoke(connection);
                        connectionCount++;
                        if (connectionCount == 1 || connectionCount % 1000 == 0)
                        {
                            var timeHandled = (connection.DepartureTime() - window.start);
                            var factor = 100 * timeHandled.TotalSeconds / window.duration.TotalSeconds;
                            Log.Information($"Loaded {connectionCount} connections (around {(int) factor}%)");
                            connectionsLoadedPing?.Invoke(connectionCount, connection.DepartureTime(), factor);
                        }
                    }

                    currentTimeTableUri = timeTable.NextTable();
                } while (timeTable.NextTableTime() <= endTime);

                Log.Information($"Added {connectionCount} connections.");
                connectionsLoadedPing?.Invoke(connectionCount, endTime, 1.0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                writer.Close();
            }
        }
    }
}