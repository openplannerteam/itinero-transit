using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.IO.LC.CSA.ConnectionProviders;
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
        /// Loads connections into the connections db and the given stops db from the given profile.
        /// </summary>
        /// <param name="connectionsDb">The connections db.</param>
        /// <param name="profile">The profile.</param>
        /// <param name="stopsDb">The stops db.</param>
        /// <param name="tripsDb">The trips db.</param>
        /// <param name="window">The window, a start time and duration.</param>
        public static void LoadConnections(this ConnectionsDb connectionsDb, 
            CSA.Profile profile, StopsDb stopsDb, TripsDb tripsDb, 
                (DateTime start, TimeSpan duration) window)
        {
            var stopsDbReader = stopsDb.GetReader();
            var tripsDbReader = tripsDb.GetReader();

            var connectionCount = 0;
            var stopCount = 0;
            var timeTable = profile.GetTimeTable(window.start);

            var tripsAdded = 0;
            
            do
            {
                foreach (var connection in timeTable.Connections())
                {
                    var stop1Uri = connection.DepartureLocation();
                    var stop1Location = profile.GetCoordinateFor(stop1Uri);
                    if (stop1Location == null)
                    {
                        continue;
                    }
                    var stop1Id = stop1Uri.ToString();
                    (uint localTileId, uint localId) stop1InternalId;
                    if (!stopsDbReader.MoveTo(stop1Id))
                    {
                        stop1InternalId = stopsDb.Add(stop1Id, stop1Location.Lon, stop1Location.Lat, 
                            new Attribute("name", stop1Location.Name));
                        stopCount++;
                    }
                    else
                    {
                        stop1InternalId = stopsDbReader.Id;
                    }

                    var stop2Uri = connection.ArrivalLocation();
                    var stop2Location = profile.GetCoordinateFor(stop2Uri);
                    if (stop2Location == null)
                    {
                        continue;
                    }
                    var stop2Id = stop2Uri.ToString();
                    (uint localTileId, uint localId) stop2InternalId;
                    if (!stopsDbReader.MoveTo(stop2Id))
                    {
                        stop2InternalId = stopsDb.Add(stop2Id, stop2Location.Lon, stop2Location.Lat, 
                            new Attribute("name", stop2Location.Name));
                        stopCount++;
                    }
                    else
                    {
                        stop2InternalId = stopsDbReader.Id;
                    }

                    var tripUri = connection.Trip().ToString();
                    var tripId = uint.MaxValue;
                    if (!tripsDbReader.MoveTo(tripUri))
                    {
                        tripId = tripsDb.Add(tripUri);
                        tripsAdded++;
                        if (tripsAdded % 250 == 0)
                        {
                            Log.Information($"{tripsAdded} trips loaded in the DB so far");
                        }
                    }
                    else
                    {
                        tripId = tripsDbReader.Id;
                    }

                    var connectionId = connection.Id().ToString();
                    connectionsDb.Add(stop1InternalId, stop2InternalId, connectionId,
                        connection.DepartureTime(),
                        (ushort) (connection.ArrivalTime() - connection.DepartureTime()).TotalSeconds, tripId);
                    connectionCount++;
                }

                if (timeTable.NextTableTime() > window.start + window.duration)
                {
                    break;
                }

                var nextTimeTableUri = timeTable.NextTable();
                timeTable = profile.GetTimeTable(nextTimeTableUri);
            } while (true);

            Log.Information($"Added {stopCount} stops and {connectionCount} connection.");
        }
    }
}