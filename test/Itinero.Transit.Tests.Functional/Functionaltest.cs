using System;
using System.ComponentModel.DataAnnotations;
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
        private static ConnectionsDb _conns;
        private static StopsDb _stops;

        private static System.Collections.Generic.Dictionary<string, ulong> _stopIds =
            new System.Collections.Generic.Dictionary<string, ulong>();


        public static string BrusselZuid = "http://irail.be/stations/NMBS/008814001";
        public static string Gent = "http://irail.be/stations/NMBS/008892007";
        public static string Brugge = "http://irail.be/stations/NMBS/008891009";
        public static string Poperinge = "http://irail.be/stations/NMBS/008896735";
        public static string Vielsalm = "http://irail.be/stations/NMBS/008845146";
        public static string Howest = "https://data.delijn.be/stops/502132";
        public static string BruggeStation2 = "https://data.delijn.be/stops/500042";
        public static string BruggeNearStation = "https://data.delijn.be/stops/507076";


        public abstract void Test();


        public static ulong GetLocation(string id)
        {
            return _stopIds[id];
        }

        public static (ConnectionsDb conns, StopsDb stops, System.Collections.Generic.Dictionary<string, ulong> mapping,
            int count)
            GetTestDb(string countStart = "", string countEnd = "")
        {
            if (_conns != null && string.IsNullOrEmpty(countStart))
            {
                return (_conns, _stops, _stopIds, -1);
            }

            var profile = Belgium.Sncb(new LocalStorage("cache"));

            var stopsDb = new StopsDb();
            var locations = profile.LocationProvider;
            foreach (var loc in locations.GetAllLocations())
            {
                var v = stopsDb.Add(loc.Uri.ToString(), loc.Lon, loc.Lat);
                _stopIds[loc.Uri.ToString()] = (ulong) v.localTileId * uint.MaxValue + v.localId;
            }

            var connectionsDb = new ConnectionsDb();
            var dayToLoad = DateTime.Now.Date.AddHours(2);
            var count = connectionsDb.LoadConnections(profile, stopsDb,
                (dayToLoad, new TimeSpan(0, 20, 0, 0)), countStart, countEnd);

            _conns = connectionsDb;
            _stops = stopsDb;
            return (_conns, _stops, _stopIds, count);
        }
    }
}