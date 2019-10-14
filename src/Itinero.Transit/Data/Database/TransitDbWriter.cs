using System;
using System.Collections.Generic;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Utils;
using Attribute = Itinero.Transit.Data.Attributes.Attribute;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// A writer for the transit db.
    /// </summary>
    public class TransitDbWriter
    {
        private readonly TransitDb _parent;
        private readonly StopsDb _stopsDb;
        private readonly IConnectionsDb _connectionsDb;
        private readonly TripsDb _tripsDb;

        internal TransitDbWriter(TransitDb parent, TransitDbSnapShot latestSnapshot)
        {
            _parent = parent;

            _stopsDb = latestSnapshot.StopsDb.Clone();
            _tripsDb = latestSnapshot.TripsDb.Clone();
            _connectionsDb = (IConnectionsDb) latestSnapshot.ConnectionsDb.Clone();
        }

        /// <summary>
        /// Adds or updates a stop.
        /// </summary>
        /// <param name="globalId">The global id.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <param name="attributes">The attributes.</param>
        /// <returns>The stop id.</returns>
        public StopId AddOrUpdateStop(string globalId, double longitude, double latitude,
            IEnumerable<Attribute> attributes = null)
        {
            var stopsDbReader = _stopsDb.GetReader();
            if (stopsDbReader.MoveTo(globalId))
            {
                return stopsDbReader.Id;
            }

            return _stopsDb.Add(globalId, longitude, latitude,
                attributes);
        }

        /// <summary>
        /// Adds or updates a new trip.
        /// </summary>
        /// <param name="globalId">The global id.</param>
        /// <param name="attributes">The attributes.</param>
        /// <returns>The trip id.</returns>
        public TripId AddOrUpdateTrip(string globalId, IEnumerable<Attribute> attributes = null)
        {
            if (_tripsDb.Get(globalId, out var found))
            {
                return found.Id;
            }

            return _tripsDb.Add(globalId, attributes);
        }

        /// <summary>
        /// Adds or updates a connection.
        /// </summary>
        /// <param name="stop1">The first stop.</param>
        /// <param name="stop2">The second stop.</param>
        /// <param name="globalId">The global id.</param>
        /// <param name="departureTime">The departure time.</param>
        /// <param name="travelTime">The travel time in seconds.</param>
        /// <param name="departureDelay">The departure delay time in seconds.</param>
        /// <param name="arrivalDelay">The arrival delay time in seconds.</param>
        /// <param name="tripId">The trip id.</param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public ConnectionId AddOrUpdateConnection(StopId stop1,
            StopId stop2, string globalId, DateTime departureTime, ushort travelTime,
            ushort departureDelay, ushort arrivalDelay, TripId tripId, ushort mode)
        {
            return _connectionsDb.AddOrUpdate(
                new Connection(
                    new ConnectionId(0, 0),
                    globalId,
                    stop1,
                    stop2,
                    departureTime.ToUnixTime(),
                    travelTime,
                    arrivalDelay,
                    departureDelay,
                    mode,
                    tripId
                )
            );
        }

        public ConnectionId AddOrUpdateConnection(Connection c)
        {
            return _connectionsDb.AddOrUpdate(c);
        }

        /// <summary>
        /// Closes this writer and commits the changes to the transit db.
        /// </summary>
        public void Close()
        {
            var latest = new TransitDbSnapShot(_stopsDb, _tripsDb, _connectionsDb);
            _parent.SetSnapshot(latest);
        }

        public void AddOrUpdateConnection(StopId stop1, StopId stop2, string globalId, ulong departureTime,
            ushort travelTime, TripId tripId)
        {
            AddOrUpdateConnection(stop1, stop2, globalId, departureTime.FromUnixTime(), travelTime, 0, 0, tripId, 0);
        }
    }
}