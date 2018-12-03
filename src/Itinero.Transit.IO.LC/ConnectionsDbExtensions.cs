using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Serilog;

namespace Itinero.IO.LC{
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
        /// <param name="window">The window, a start time and duration.</param>
        public static void LoadConnections(this ConnectionsDb connectionsDb, Profile<TransferStats> profile,
            StopsDb stopsDb, (DateTime start, TimeSpan duration) window)
        {
            var stopsDbReader = stopsDb.GetReader();

            var trips = new Dictionary<string, uint>();
            
            var cc = 0;
            var sc = 0;
            var timeTable = profile.GetTimeTable(window.start);
            do
            {
                foreach (var connection in timeTable.Connections())
                {
                    var stop1Uri = connection.DepartureLocation();
                    var stop1Location = profile.GetCoordinateFor(stop1Uri);
                    var stop1Id = stop1Uri.ToString();
                    (uint localTileId, uint localId) stop1InternalId;
                    if (!stopsDbReader.MoveTo(stop1Id))
                    {
                        stop1InternalId = stopsDb.Add(stop1Id, stop1Location.Lon, stop1Location.Lon);
                        sc++;
                    }
                    else
                    {
                        stop1InternalId = stopsDbReader.Id;
                    }

                    var stop2Uri = connection.ArrivalLocation();
                    var stop2Location = profile.GetCoordinateFor(stop2Uri);
                    var stop2Id = stop2Uri.ToString();
                    (uint localTileId, uint localId) stop2InternalId;
                    if (!stopsDbReader.MoveTo(stop2Id))
                    {
                        stop2InternalId = stopsDb.Add(stop2Id, stop2Location.Lon, stop2Location.Lon);
                        sc++;
                    }
                    else
                    {
                        stop2InternalId = stopsDbReader.Id;
                    }

                    var tripUri = connection.Trip().ToString();
                    if (!trips.TryGetValue(tripUri, out var tripId))
                    {
                        tripId = (uint) trips.Count;
                        trips[tripUri] = tripId;
                        
                        //Log.Information($"Added new trip {tripUri} with {tripId}");
                    }

                    var connectionId = connection.Id().ToString();
                    connectionsDb.Add(stop1InternalId, stop2InternalId, connectionId,
                        connection.DepartureTime(),
                        (ushort) (connection.ArrivalTime() - connection.DepartureTime()).TotalSeconds, tripId);
                    cc++;
                }

                if ((timeTable.NextTableTime() - window.start) > window.duration)
                {
                    break;
                }

                var nextTimeTableUri = timeTable.NextTable();
                timeTable = profile.GetTimeTable(nextTimeTableUri);
            } while (true);

            Log.Information($"Added {sc} stops and {cc} connection.");
        }
    }
}