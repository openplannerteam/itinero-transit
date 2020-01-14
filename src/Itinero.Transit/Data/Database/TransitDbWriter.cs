using System.Collections.Generic;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// A writer for the transit db.
    /// </summary>
    public class TransitDbWriter : IGlobalId
    {
        private readonly TransitDb _parent;

        public readonly IStopsDb StopsDb;
        public readonly IConnectionsDb ConnectionsDb;
        public readonly ITripsDb TripsDb;

        /// <summary>
        /// The URL (or prefix) of the PT-operator
        /// </summary>
        public string GlobalId { get; set; }

        /// <summary>
        /// Attributes of the PT-operator.
        /// Is moved into the TDB afterwards
        /// </summary>
        public Dictionary<string, string> AttributesWritable { get; set; }

        public IReadOnlyDictionary<string, string> Attributes => AttributesWritable;

        internal TransitDbWriter(TransitDb parent, TransitDbSnapShot latestSnapshot)
        {
            _parent = parent;

            GlobalId = _parent.Latest.GlobalId;
            AttributesWritable = new Dictionary<string, string>();
            foreach (var kv in _parent.Latest.Attributes)
            {
                AttributesWritable.Add(kv.Key, kv.Value);
            }

            StopsDb = latestSnapshot.StopsDb.Clone();
            TripsDb = latestSnapshot.TripsDb.Clone();
            ConnectionsDb = latestSnapshot.ConnectionsDb.Clone();
        }


        /// <summary>
        /// Closes this writer and commits the changes to the transit db.
        /// </summary>
        public void Close()
        {
            StopsDb.PostProcess();
            ConnectionsDb.PostProcess();
            TripsDb.PostProcess();

            var latest = new TransitDbSnapShot(_parent.DatabaseId, GlobalId, StopsDb, ConnectionsDb, TripsDb, Attributes);
            _parent.SetSnapshot(latest, GlobalId, Attributes);
        }

        public StopId AddOrUpdateStop(Stop stop)
        {
            return ((IDatabase<StopId, Stop>) StopsDb).AddOrUpdate(stop);
        }

        public ConnectionId AddOrUpdateConnection(Connection connection)
        {
            return ((IDatabase<ConnectionId, Connection>) ConnectionsDb).AddOrUpdate(connection);
        }

        public TripId AddOrUpdateTrip(Trip trip)
        {
            return ((IDatabase<TripId, Trip>) TripsDb).AddOrUpdate(trip);
        }

        public TripId AddOrUpdateTrip(string globalId)
        {
            return AddOrUpdateTrip(new Trip(globalId));
        }
    }
}