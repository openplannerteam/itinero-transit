using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// A writer for the transit db.
    /// </summary>
    public class TransitDbWriter
    {
        private readonly TransitDb _parent;

        private readonly IStopsDb _stopsDb;
        private readonly IConnectionsDb _connectionsDb;
        private readonly ITripsDb _tripsDb;

        internal TransitDbWriter(TransitDb parent, TransitDbSnapShot latestSnapshot)
        {
            _parent = parent;

            _stopsDb = latestSnapshot.StopsDb.Clone();
            _tripsDb = latestSnapshot.TripsDb.Clone();
            _connectionsDb = latestSnapshot.ConnectionsDb.Clone();
        }


        /// <summary>
        /// Closes this writer and commits the changes to the transit db.
        /// </summary>
        public void Close()
        {
            _stopsDb.PostProcess();
            _connectionsDb.PostProcess();
            _tripsDb.PostProcess();

            var latest = new TransitDbSnapShot(_parent.DatabaseId, _stopsDb, _connectionsDb, _tripsDb);
            _parent.SetSnapshot(latest);
        }

        public StopId AddOrUpdateStop(Stop stop)
        {
            return ((IDatabase<StopId, Stop>) _stopsDb).AddOrUpdate(stop);
        }
        
        public ConnectionId AddOrUpdateConnection(Connection connection)
        {
            return ((IDatabase<ConnectionId, Connection>) _connectionsDb).AddOrUpdate(connection);
        }
        public TripId AddOrUpdateTrip(Trip trip)
        {         return ((IDatabase<TripId, Trip>) _tripsDb).AddOrUpdate(trip);

            
        }
        public TripId AddOrUpdateTrip(string globalId)
        {
            return AddOrUpdateTrip(new Trip(globalId));
        }
    }
}