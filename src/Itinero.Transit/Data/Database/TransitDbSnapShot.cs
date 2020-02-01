using System;
using System.Collections.Generic;
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
    public class TransitDbSnapShot : IGlobalId
    {
        public string GlobalId { get; }
        public IReadOnlyDictionary<string, string> Attributes { get; }
        
        public uint Id { get; }

        public IStopsDb StopsDb { get; }
        public ITripsDb TripsDb { get; }
        public IConnectionsDb ConnectionsDb { get; }
        public IOperatorDb OperatorDb { get; }

        internal TransitDbSnapShot(uint id,
            string globalId,
            IStopsDb stopsDb, 
            IConnectionsDb connectionsDb, 
            ITripsDb tripsDb,
            IOperatorDb operatorDb,
            IReadOnlyDictionary<string, string> attributes = null)
        {
            Id = id;
            GlobalId = globalId;
            StopsDb = stopsDb;
            TripsDb = tripsDb;
            ConnectionsDb = connectionsDb;
            OperatorDb = operatorDb;
            Attributes = attributes ?? new Dictionary<string, string>();
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

        public void WriteTo(Stream stream)
        {
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, GlobalId);
            formatter.Serialize(stream, Attributes);

            stream.Serialize(OperatorDb, formatter);
            stream.Serialize(StopsDb, formatter);
            stream.Serialize(TripsDb, formatter);
            stream.Serialize(ConnectionsDb, formatter);
        }
    }
}