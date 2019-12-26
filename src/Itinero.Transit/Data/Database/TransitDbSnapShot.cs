using System.IO;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// A transit db snapshot, represents a consistent state of the transit db.
    /// </summary>
    public class TransitDbSnapShot
    {
        
        public uint Id { get; }

        public IStopsDb StopsDb { get; }
        public ITripsDb TripsDb { get; }
        public IConnectionsDb ConnectionsDb { get; }
        internal TransitDbSnapShot(uint dbId, IStopsDb stopsDb,  IConnectionsDb connectionsDb, ITripsDb tripsDb)
        {
            Id = dbId;
            StopsDb = stopsDb;
            TripsDb = tripsDb;
            ConnectionsDb = connectionsDb;
        }

        public void WriteTo(FileStream stream)
        {
            // TODO FIXME IMPLEMENT ME
            throw new System.NotImplementedException();
        }
    }
}