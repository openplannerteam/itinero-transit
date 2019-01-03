using System;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.CSA.ConnectionProviders;
using Itinero.Transit.IO.LC.CSA.Connections;
using Itinero.Transit.Logging;
using Attribute = Itinero.Transit.Data.Attributes.Attribute;

namespace Itinero.Transit.IO.LC
{
    /// <summary>
    /// Contains extensions methods related to the connections db.
    /// </summary>
    public static class ConnectionsDbExtensions
    {
        /// <summary>
        /// Adds the given stop to the DB. Returns the internal ID
        /// </summary>
        /// <returns></returns>
        private static (uint tileId, uint localId)
            AddStop(ILocationProvider profile, Uri stopUri, StopsDb stopsDb, StopsDb.StopsDbReader stopsDbReader)
        {
            var stop1Uri = stopUri;
            var stop1Location = profile.GetCoordinateFor(stop1Uri);
            if (stop1Location == null)
            {
                return (uint.MaxValue, uint.MaxValue);
            }

            var stop1Id = stop1Uri.ToString();

            if (stopsDbReader.MoveTo(stop1Id))
            {
                return stopsDbReader.Id;
            }
            return stopsDb.Add(stop1Id, stop1Location.Lon, stop1Location.Lat,
                new Attribute("name", stop1Location.Name));

        }

        private static uint AddTrip(LinkedConnection connection, TripsDb tripsDb, TripsDb.TripsDbReader tripsDbReader)
        {
            var tripUri = connection.Trip().ToString();
            if (tripsDbReader.MoveTo(tripUri))
            {
                return tripsDbReader.Id;
            }

            var attributes = new AttributeCollection(
                new Attribute("headsign", connection.Direction),
                new Attribute("trip", connection.Trip().ToString()),
                new Attribute("route", connection.Route().ToString())
            );
            return tripsDb.Add(tripUri, attributes);
        }


        private static void AddConnection(LinkedConnection connection, Profile profile, StopsDb stopsDb,
            StopsDb.StopsDbReader stopsDbReader, ConnectionsDb connectionsDb, TripsDb tripsDb,
            TripsDb.TripsDbReader tripsDbReader)
        {
            var stop1Id = AddStop(profile, connection.DepartureLocation(), stopsDb, stopsDbReader);
            var stop2Id = AddStop(profile, connection.ArrivalLocation(), stopsDb, stopsDbReader);

            if (stop1Id.localId == uint.MaxValue && stop1Id.tileId == uint.MaxValue &&
                stop2Id.localId == uint.MaxValue && stop2Id.tileId == uint.MaxValue)
            {
                return;
            }

            var tripId = AddTrip(connection, tripsDb, tripsDbReader);


            var connectionUri = connection.Id().ToString();
            connectionsDb.Add(stop1Id, stop2Id, connectionUri,
                connection.DepartureTime(),
                (ushort) (connection.ArrivalTime() - connection.DepartureTime()).TotalSeconds, 
                tripId);
        }
        

        /// <summary>
        /// Loads connections into the connections db and the given stops db from the given profile.
        /// </summary>
        /// <param name="connectionsDb">The connections db.</param>
        /// <param name="profile">The profile.</param>
        /// <param name="stopsDb">The stops db.</param>
        /// <param name="tripsDb">The trips db.</param>
        /// <param name="window">The window, a start time and duration.</param>
        public static void LoadConnections(this ConnectionsDb connectionsDb,
            Profile profile, StopsDb stopsDb, TripsDb tripsDb,
            (DateTime start, TimeSpan duration) window)
        {
            Log.Information("Building the database... Hang on");
            var stopsDbReader = stopsDb.GetReader();
            var tripsDbReader = tripsDb.GetReader();

            var connectionCount = 0;
            var timeTable = profile.GetTimeTable(window.start);

            var tripsAdded = 0;

            var locations = profile.GetAllLocations();
            var i = 0;
            var length = locations.Count();
            foreach (var l in locations)
            {
                stopsDb.Add(l.Uri.ToString(), l.Lon, l.Lat,new Attribute("name", l.Name));
                i++;
                if (i % 100 == 0)
                {
                    Log.Verbose($"Added location {i}/{length}");
                }
            }
            
            do
            {
                foreach (var connection in timeTable.Connections())
                {
                    AddConnection(connection, profile, stopsDb, stopsDbReader, connectionsDb, tripsDb, tripsDbReader);
                    connectionCount++;
                    if (connectionCount % 1000 == 0)
                    {
                        var timeHandled = (connection.DepartureTime() - window.start);
                        var factor = 100 * timeHandled.TotalSeconds / window.duration.TotalSeconds;
                        Log.Information($"Loaded {connectionCount} connections (around {(int) factor}%)");
                    }
                }

                if (timeTable.NextTableTime() > window.start + window.duration)
                {
                    break;
                }

                var nextTimeTableUri = timeTable.NextTable();
                timeTable = profile.GetTimeTable(nextTimeTableUri);
            } while (true);

            Log.Information($"Added {connectionCount} connections.");
        }

        

        
    }
}