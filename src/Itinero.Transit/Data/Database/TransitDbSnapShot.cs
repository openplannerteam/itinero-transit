using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.Serialization;
using Itinero.Transit.Utils;

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

        public Connection Get(ConnectionId id)
        {
            return ConnectionsDb.Get(id);
        }

        public Stop Get(StopId id)
        {
            return StopsDb.Get(id);
        }

        public Trip Get(TripId trip)
        {
            return TripsDb.Get(trip);
        }

        public DateTime EarliestDate()
        {
            return ConnectionsDb.EarliestDate.FromUnixTime();
        }

        
        public DateTime LatestDate()
        {
            return ConnectionsDb.LatestDate.FromUnixTime();
        }
        
        public void WriteTo(FileStream stream)
        {
            var formatter = new BinaryFormatter();
            stream.Serialize(StopsDb, formatter);
            stream.Serialize(TripsDb, formatter);
            stream.Serialize(ConnectionsDb, formatter);
        }
    }
}