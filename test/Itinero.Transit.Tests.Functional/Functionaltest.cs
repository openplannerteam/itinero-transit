using System;
using Itinero.IO.LC;
using Itinero.Transit.Data;
using Itinero.Transit.Tests.Functional.Performance;
using Reminiscence.Collections;
using Serilog;
// ReSharper disable UnusedMember.Global

namespace Itinero.Transit.Tests.Functional
{
    public abstract class FunctionalTest
    {
        public static readonly List<FunctionalTest> tests = new List<FunctionalTest>();
        private static ConnectionsDb _conns;
        private static StopsDb _stops;

        private static System.Collections.Generic.Dictionary<string, (uint localTileId, uint localId)> _stopIds =
            new System.Collections.Generic.Dictionary<string, (uint localTileId, uint localId)>();


        public static string BrusselZuid = "http://irail.be/stations/NMBS/008814001";
        public static string Gent = "http://irail.be/stations/NMBS/008892007";
        public static string Brugge = "http://irail.be/stations/NMBS/008891009";
        public static string Poperinge = "http://irail.be/stations/NMBS/008896735";
        public static string Vielsalm = "http://irail.be/stations/NMBS/008845146";
        public static string Howest = "https://data.delijn.be/stops/502132";
        public static string BruggeStation2 = "https://data.delijn.be/stops/500042";
        public static string BruggeNearStation = "https://data.delijn.be/stops/507076";


        public FunctionalTest()
        {
            tests.Add(this);
        }

        public abstract void Test();

        public static (ConnectionsDb conns, StopsDb stops, 
            System.Collections.Generic.Dictionary<string, (uint localTileId, uint localId)> mapping)
            GetTestDb()
        {
            if (_conns != null)
            {
                return (_conns, _stops, _stopIds);
            }

            var profile = Belgium.Sncb(new LocalStorage("cache"));

            // create a stops db and connections db.
            var connectionsDb = new ConnectionsDb();
            var dayToLoad = DateTime.Now.Date.AddHours(2);
            connectionsDb.LoadConnections(profile, stopsDb,
                (dayToLoad, new TimeSpan(0, 20, 0, 0)));


            var locations = profile.LocationProvider;

            var stopsDb = new StopsDb();
            foreach (var loc in locations.GetAllLocations())
            {
                var v = stopsDb.Add(loc.Uri.ToString(), loc.Lon, loc.Lat);
                _stopIds[loc.Uri.ToString()] = v;
            }

            _conns = connectionsDb;
            _stops = stopsDb;
            return (_conns, _stops, _stopIds);
        }
    }
}