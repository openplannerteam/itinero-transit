using System.Collections.Generic;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.Simple;

namespace Itinero.Transit.Data.Compacted
{
    /// <summary>
    ///  The compacted writer is a transitdb writer that attempts to compact the trips.
    ///
    /// If two trips have the exact same stops (in the same order), these stops will only be saved once.
    /// Connections are generated on the fly, and have an id from which the trip and time can be deduced
    ///
    /// In an initial version, routes have to be added on beforehand
    /// 
    /// </summary>
    public class CompactedWriter
    {
        private readonly uint _databaseid;
        public string GlobalId { get; private set; }

        private readonly Dictionary<string, string> _attributesWritable = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> Attributes => _attributesWritable;


        public IStopsDb Stops => _stopsDb;
        private SimpleStopsDb _stopsDb;

        public IRoutesDb RoutesDb => _routesDb;
        private SimpleRoutesDb _routesDb;


        public CompactedWriter(uint databaseid, string globalId)
        {
            GlobalId = globalId;
            _databaseid = databaseid;
            _stopsDb = new SimpleStopsDb(databaseid);
            _routesDb = new SimpleRoutesDb(databaseid);
        }

        public StopId AddOrUpdateStop(Stop stop)
        {
            return _stopsDb.AddOrUpdate(stop);
        }


        public void SetAttribute(string key, string value)
        {
            _attributesWritable[key] = value;
        }

        public void SetGlobalId(string key)
        {
            GlobalId = key;
        }


        /// <summary>
        /// Adds a full route to the database, returns the id for this route.
        /// Note that the route might already exist, in which case the already existing ID is added
        /// </summary>
        public RouteId AddOrUpdateRoute(Route route)
        {
            return _routesDb.AddOrUpdate(route);
        }
    }
}