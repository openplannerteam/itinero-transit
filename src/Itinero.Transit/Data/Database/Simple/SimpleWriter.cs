using System;
using System.Collections.Generic;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.Simple;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// A writer for the transit db.
    /// </summary>
    public class SimpleWriter : IWriter
    {
        /// <summary>
        /// The URL (or prefix) of the PT-operator
        /// </summary>
        public string GlobalId { get; private set; }
        
        /// <summary>
        /// Attributes of the PT-operator.
        /// </summary>
        private Dictionary<string, string> AttributesWritable { get; }

        public IReadOnlyDictionary<string, string> Attributes => AttributesWritable;

        private readonly uint _databaseId;

        public IStopsDb Stops { get; }
        public IConnectionsDb Connections { get; }
        public ITripsDb Trips { get; }


       

        public SimpleWriter(uint databaseId, 
            IGlobalId attributes,
            IStopsDb stops,
            IConnectionsDb connections,
            ITripsDb trips
            )
        {
            _databaseId = databaseId;
            AttributesWritable = new Dictionary<string, string>();
            this.CopyAttributesFrom(attributes);
            Stops = stops;
            Trips = trips;
            Connections = connections;
        }
        
        public SimpleWriter(uint databaseId, string globalId)
        {
            _databaseId = databaseId;
            SetGlobalId(globalId);
            AttributesWritable = new Dictionary<string, string>();

            Stops = new SimpleStopsDb(databaseId);
            Trips = new SimpleTripsDb(databaseId);
            Connections = new SimpleConnectionsDb(databaseId);
        }


        /// <summary>
        /// Closes this writer and commits the changes to the transit db.
        /// </summary>
        public TransitDbSnapShot GetSnapshot()
        {
            if (string.IsNullOrEmpty(GlobalId))
            {
                throw new ArgumentException("The global id is not set for this writer");
            }
            Stops.PostProcess();
            Connections.PostProcess();
            Trips.PostProcess();


            return new TransitDbSnapShot(_databaseId, GlobalId, Stops, Connections, Trips, Attributes);
        }


        public void SetGlobalId(string key)
        {
            GlobalId = key;
        }
        public void SetAttribute(string key, string value)
        {
            if (value != null)
            {
                AttributesWritable[key] = value;
            }
        }

        public StopId AddOrUpdateStop(Stop stop)
        {
            return ((IDatabase<StopId, Stop>) Stops).AddOrUpdate(stop);
        }

        public ConnectionId AddOrUpdateConnection(Connection connection)
        {
            return ((IDatabase<ConnectionId, Connection>) Connections).AddOrUpdate(connection);
        }

        public TripId AddOrUpdateTrip(Trip trip)
        {
            return ((IDatabase<TripId, Trip>) Trips).AddOrUpdate(trip);
        }

        public TripId AddOrUpdateTrip(string globalId)
        {
            return AddOrUpdateTrip(new Trip(globalId));
        }
    }
}