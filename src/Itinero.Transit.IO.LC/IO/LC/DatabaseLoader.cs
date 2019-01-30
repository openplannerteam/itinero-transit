using System;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.CSA.ConnectionProviders;
using Itinero.Transit.IO.LC.CSA.Connections;
using Itinero.Transit.IO.LC.CSA.Data;
using Itinero.Transit.IO.LC.CSA.LocationProviders;
using Attribute = Itinero.Transit.Data.Attributes.Attribute;

namespace Itinero.Transit.IO.LC
{
    public static class ProfileExtensions
    {
        /// <summary>
        /// Dumps all the information between the dates into the databases.
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="stops"></param>
        /// <param name="connections"></param>
        /// <param name="trips"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="onLocationHandled">Callback when a location is added</param>
        /// <param name="onTimeTableHandled">Callback when a timetable has been handled</param>
        public static void AddDataTo(this Profile profile, StopsDb stops, ConnectionsDb connections, TripsDb trips,
            DateTime start, DateTime end, Action<string> onError,
            LoggingOptions onLocationHandled = null,
            LoggingOptions onTimeTableHandled = null)
        {
            var dbs = new DatabaseLoader(stops, trips, connections, onLocationHandled, onTimeTableHandled, onError);
            dbs.AddAllLocations(profile);
            dbs.AddAllConnections(profile, start, end);
        }
    }

    public class LoggingOptions
    {
        internal readonly Action<(int currentCount, int batchTarget, int nrOfBatches)>
            OnAdded;

        internal readonly int TriggerEvery;

        public LoggingOptions(Action<(int currentCount, int batchTarget, int nrOfBatches)> onAdded,
            int triggerEvery = 100)
        {
            OnAdded = onAdded;
            TriggerEvery = triggerEvery;
        }

        internal void Ping(int currentCount, int batchTarget, int batchNr)
        {
            if (currentCount % TriggerEvery == 0)
            {
                OnAdded.Invoke((currentCount, batchTarget, batchNr));
            }
        }
    }

    /// <summary>
    /// Small helper class to bundle all the databases together
    /// </summary>
    public class DatabaseLoader
    {
        private readonly StopsDb _stops;
        private readonly StopsDb.StopsDbReader _stopsReader;


        private readonly TripsDb _trips;
        private readonly TripsDb.TripsDbReader _tripsReader;

        private readonly ConnectionsDb _connections;
        private readonly ConnectionsDb.ConnectionsDbReader _connectionsReader;

        private readonly LoggingOptions _locationsLogger, _connectionsLogger;
        private readonly Action<string> _onError;

        public DatabaseLoader(StopsDb stops, TripsDb trips, ConnectionsDb connections, LoggingOptions locationsLogger,
            LoggingOptions connectionsLogger, Action<string> onError)
        {
            _stops = stops;
            _stopsReader = stops.GetReader();
            _trips = trips;
            _tripsReader = trips.GetReader();
            _connections = connections;
            _locationsLogger = locationsLogger;
            _connectionsLogger = connectionsLogger;
            _onError = onError;
            _connectionsReader = connections.GetReader();
        }


        public void AddAllLocations(Profile profile)
        {
            var count = 0;
            var batchCount = 0;
            foreach (var locationsFragment in profile.LocationProvider)
            {
                batchCount++;
                foreach (var location in locationsFragment.Locations)
                {
                    AddLocation(location);
                    count++;
                    _locationsLogger?.Ping(count, batchCount, profile.LocationProvider.Count);
                }
            }
        }

        public void AddAllConnections(Profile p, DateTime startDate, DateTime endDate)
        {
            for (int i = 0; i < p.ConnectionsProvider.Count; i++)
            {
                var cons = p.ConnectionsProvider[i];
                var loc = p.LocationProvider[i];
                AddTimeTableWindow(cons, loc, startDate, endDate);
            }
        }


        private void AddTimeTableWindow(ConnectionProvider cons, LocationProvider locations,
            DateTime startDate, DateTime endDate, int batchNr = 1)
        {
            var currentTimeTableUri = cons.TimeTableIdFor(startDate);

            var count = 0;
            TimeTable timeTable;
            do
            {
                timeTable = cons.GetTimeTable(currentTimeTableUri);

                count++;
                AddTimeTable(timeTable, locations);

                var estimatedCount = (float) (timeTable.EndTime().Ticks - startDate.Ticks) /
                                     (endDate.Ticks - startDate.Ticks);
                _connectionsLogger?.Ping(count, (int) (count / estimatedCount), batchNr);

                currentTimeTableUri = timeTable.NextTable();
            } while (timeTable.EndTime() < endDate);
        }

        /// <summary>
        /// Adds the entire time table
        /// </summary>
        private void AddTimeTable(TimeTable tt, LocationProvider locations)
        {
            tt.Validate(locations, (connection, uri) =>
                {
                    _onError.Invoke($"A connection uses a unknown location {uri}\nThe connection is {connection}");
                    return false;
                },
                connection =>
                {
                    _onError.Invoke("A connection is mentioned multiple times");
                    return false;
                },
                (connection, errorMsg) =>
                {
                    _onError(errorMsg);
                    return false;
                }
            );
            foreach (var connection in tt.Connections())
            {
                AddConnection(connection, locations);
            }
        }


        /// <summary>
        /// Adds the LOCATION metadata to the StopsDB
        /// </summary>
        /// <param name="location"></param>
        /// <param name="stopsDb"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        private (uint, uint) AddLocation(Location location)
        {
            var globalId = location.Id();

            var stop1Id = globalId.ToString();
            if (_stopsReader.MoveTo(stop1Id))
            {
                // Already in the Database
                return _stopsReader.Id;
            }

            return _stops.Add(stop1Id, location.Lon, location.Lat,
                new Attribute("name", location.Name));
        }


        /// <summary>
        /// Adds the given stop to the DB. Returns the internal ID
        /// </summary>
        /// <returns></returns>
        private (uint tileId, uint localId)
            AddStop(LocationProvider profile, Uri stopUri)
        {
            var location = profile.GetCoordinateFor(stopUri);
            if (location == null)
            {
                throw new ArgumentException($"Location {stopUri} not found. Run validation first please!");
            }

            var id = stopUri.ToString();

            if (_stopsReader.MoveTo(id))
            {
                return _stopsReader.Id;
            }

            return _stops.Add(id, location.Lon, location.Lat,
                new Attribute("name", location.Name));
        }


        /// <summary>
        /// Adds the connection to the connectionsDB
        /// </summary>
        internal void AddConnection(Connection connection,
            LocationProvider locations)
        {
            var stop1Id = AddStop(locations, connection.DepartureLocation());
            var stop2Id = AddStop(locations, connection.ArrivalLocation());

            var tripId = AddTrip(connection);

            var connectionUri = connection.Id().ToString();

            _connections.Add(stop1Id, stop2Id, connectionUri,
                connection.DepartureTime(),
                (ushort) (connection.ArrivalTime() - connection.DepartureTime()).TotalSeconds,
                tripId);
        }


        /// <summary>
        /// Adds the TRIP metadata to the trip db
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="tripsDb"></param>
        /// <param name="tripsDbReader"></param>
        /// <returns></returns>
        private uint AddTrip(Connection connection)
        {
            var tripUri = connection.Trip().ToString();
            if (_tripsReader.MoveTo(tripUri))
            {
                return _tripsReader.Id;
            }

            var attributes = new AttributeCollection(
                new Attribute("headsign", connection.Direction),
                new Attribute("trip", connection.Trip().ToString()),
                new Attribute("route", connection.Route().ToString())
            );
            return _trips.Add(tripUri, attributes);
        }
    }
}