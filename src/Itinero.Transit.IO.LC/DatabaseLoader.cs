using System;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.IO.LC.Data;
using Attribute = Itinero.Transit.Data.Attributes.Attribute;
using Connection = Itinero.Transit.IO.LC.Data.Connection;

namespace Itinero.Transit.IO.LC
{
    /// <summary>
    /// Small helper class to bundle all the databases together
    /// </summary>
    internal class DatabaseLoader
    {
        private readonly LoggingOptions _locationsLogger, _connectionsLogger;
        private readonly Action<string> _onError;

        private readonly TransitDb.TransitDbWriter _writer;

        /// <inheritdoc />
        public DatabaseLoader(TransitDb.TransitDbWriter writer, LoggingOptions locationsLogger,
            LoggingOptions connectionsLogger, Action<string> onError)
        {
            _writer = writer;

            _locationsLogger = locationsLogger;
            _connectionsLogger = connectionsLogger;
            _onError = onError;
            if (onError == null)
            {
                throw new ArgumentNullException(nameof(onError));
            }
        }


        public void AddAllLocations(LinkedConnectionDataset linkedConnectionDataset)
        {
            var count = 0;
            var batchCount = 1;
            foreach (var locationsFragment in linkedConnectionDataset.LocationProvider)
            {
                batchCount++;
                foreach (var location in locationsFragment.Locations)
                {
                    AddLocation(location);
                    count++;
                    _locationsLogger?.Ping(count, locationsFragment.Locations.Count, batchCount,
                        linkedConnectionDataset.LocationProvider.Count);
                }

                _locationsLogger?.Ping(count, locationsFragment.Locations.Count, batchCount,
                    linkedConnectionDataset.LocationProvider.Count);
            }
        }

        public (int loaded, int reused) AddAllConnections(LinkedConnectionDataset p, DateTime startDate, DateTime endDate)
        {
            if (startDate.Kind != DateTimeKind.Utc || endDate.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Please provide all dates in UTC");
            }
            
            var loaded = 0;
            var reused = 0;
            for (var i = 0; i < p.ConnectionsProvider.Count; i++)
            {
                var cons = p.ConnectionsProvider[i];
                var loc = p.LocationProvider[i];
                var (l, r) = AddTimeTableWindow(cons, loc, startDate, endDate, i, p.ConnectionsProvider.Count);
                loaded += l;
                reused += r;
            }

            return (loaded, reused);
        }


        private (int loaded, int ofWhichReused) AddTimeTableWindow(ConnectionProvider cons, LocationProvider locations,
            DateTime startDate, DateTime endDate, int batchNr, int totalBatches)
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
                    AddTimeTable(timeTable, locations);
                }
                else
                {
                    reused++;
                }

                var estimatedCount = (float) (timeTable.EndTime().Ticks - startDate.Ticks) /
                                     (endDate.Ticks - startDate.Ticks);
                _connectionsLogger?.Ping(count, (int) (count / estimatedCount), batchNr, totalBatches);

                currentTimeTableUri = timeTable.NextTable();
            } while (timeTable.EndTime() < endDate);

            return (count, reused);
        }

        /// <summary>
        /// Adds the entire time table.
        /// </summary>
        private void AddTimeTable(TimeTable tt, LocationProvider locations)
        {
            tt.Validate(locations, (connection, uri) =>
                {
                    _onError($"A connection uses an unknown location {uri}\nThe connection is {connection}");
                    return false;
                },
                connection =>
                {
                    _onError($"A connection is mentioned multiple times: {connection.Uri}");
                    return true;
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
        /// Adds the location metadata to the StopsDB
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        private StopId AddLocation(Location location)
        {
            var globalId = location.Uri;
            var stopId = globalId.ToString();

            var attributes = new AttributeCollection();
            attributes.AddOrReplace("name", location.Name);
            // ReSharper disable once InvertIf
            if (location.Names != null)
            {
                foreach (var (lang, name) in location.Names)
                {
                    attributes.AddOrReplace($"name:{lang}", name);
                }
            }

            return _writer.AddOrUpdateStop(stopId, location.Lon, location.Lat, attributes);
        }

        /// <summary>
        /// Adds the given stop to the DB. Returns the internal ID
        /// </summary>
        /// <returns></returns>
        private StopId
            AddStop(LocationProvider profile, Uri stopUri)
        {
            var location = profile.GetCoordinateFor(stopUri);
            if (location == null)
            {
                throw new ArgumentException($"Location {stopUri} not found. Run validation first please!");
            }

            return AddLocation(location);
        }


        /// <summary>
        /// Adds the connection to the connectionsDB
        /// </summary>
        private void AddConnection(Connection connection,
            LocationProvider locations)
        {
            var stop1Id = AddStop(locations, connection.DepartureLocation());
            var stop2Id = AddStop(locations, connection.ArrivalLocation());

            var tripId = AddTrip(connection);

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

            _writer.AddOrUpdateConnection(stop1Id, stop2Id, connectionUri,
                connection.DepartureTime(),
                (ushort) (connection.ArrivalTime() - connection.DepartureTime()).TotalSeconds,
                connection.DepartureDelay, connection.ArrivalDelay, tripId, mode);
        }


        /// <summary>
        /// Adds the TRIP metadata to the trip db
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        private TripId AddTrip(Connection connection)
        {
            var tripUri = connection.Trip().ToString();

            var attributes = new AttributeCollection(
                new Attribute("headsign", connection.Direction),
                new Attribute("trip", $"{connection.Trip()}"),
                new Attribute("route", $"{connection.Route()}")
            );
            return _writer.AddOrUpdateTrip(tripUri, attributes);
        }
    }
}