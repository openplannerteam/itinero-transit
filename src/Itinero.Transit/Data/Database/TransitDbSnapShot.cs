using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Itinero.Transit.Data.Compacted;
using Itinero.Transit.Data.Serialization;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// A transit db snapshot, represents a consistent and readonly state of the transit db.
    /// </summary>
    public class TransitDbSnapShot : IGlobalId
    {

        public static IWriter CreateSimple(uint databaseId, string globalId)
        {
            return new SimpleWriter(databaseId, globalId);
        }

        public static CompactedWriter CreateCompactedWriter(uint databaseid, string globalId)
        {
            return new CompactedWriter(databaseid, globalId);
        }
            
        public string GlobalId { get; }
        public IReadOnlyDictionary<string, string> Attributes { get; }
        /// <summary>
        /// The identifier of the database. Should be unique amongst the program
        /// </summary>
        public uint DatabaseId { get; }

        public IStopsDb Stops { get; }
        public ITripsDb Trips { get; }
        public IConnectionsDb Connections { get; }

        internal TransitDbSnapShot(uint id,
            string globalId,
            IStopsDb stopsDb, 
            IConnectionsDb connectionsDb, 
            ITripsDb tripsDb,
            IReadOnlyDictionary<string, string> attributes = null)
        {
            DatabaseId = id;
            GlobalId = globalId;
            Stops = stopsDb;
            Trips = tripsDb;
            Connections = connectionsDb;
            Attributes = attributes ?? new Dictionary<string, string>();
        }

        public DateTime EarliestDate()
        {
            return Connections.EarliestDate.FromUnixTime();
        }


        public DateTime LatestDate()
        {
            return Connections.LatestDate.FromUnixTime();
        }

        
        /// <summary>
        /// Creates a new writer which uses the same underlying technology as this snapshot.
        /// WHen the changes are made, call 'Writer.GetSnapshot()' to get a new and finished snapshot.
        /// This snapshot will not be changed at all and can be safely used in a threadsafe way
        /// </summary>
        public IWriter Edit()
        {
            return new SimpleWriter(DatabaseId,
                this,
                Stops.Clone(), Connections.Clone(), Trips.Clone());
        }

        public void WriteTo(Stream stream)
        {
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, GlobalId);
            formatter.Serialize(stream, Attributes);

            stream.Serialize(Stops, formatter);
            stream.Serialize(Trips, formatter);
            stream.Serialize(Connections, formatter);
        }
    }
}