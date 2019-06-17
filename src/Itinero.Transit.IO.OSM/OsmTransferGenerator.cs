using System;
using System.Collections.Generic;
using Itinero.IO.Osm.Tiles;
using Itinero.Profiles;
using Itinero.Profiles.Lua.Osm;
using Itinero.Transit.Data;
using Itinero.Transit.OtherMode;

namespace Itinero.Transit.IO.OSM
{
    /// <inheritdoc />
    /// <summary>
    /// The transfer generator has the responsibility of creating
    /// transfers between multiple locations, possibly intermodal.
    /// If the departure and arrival location are the same, an internal
    /// transfer is generate.
    /// If not, the OpenStreetMap database is queried to generate a path between them.
    /// </summary>
    public class OsmTransferGenerator : IOtherModeGenerator
    {
        private readonly RouterDb _routerDb;
        private readonly Profile _profile;

        private const float _searchDistance = 50f;

        ///  <summary>
        ///  Generate a new transfer generator, which takes into account
        ///  the time needed to transfer, walk, ...
        /// 
        ///  Footpaths are generated using an Osm-based router database
        ///  </summary>
        ///  <param name="walkingProfile">The vehicle profile, default is pedestrian.</param>
        ///  <param name="baseTilesUrl">The base tile url.</param>
        public OsmTransferGenerator(Profile walkingProfile = null,
            string baseTilesUrl = "https://tiles.openplanner.team/planet")
        {
            _profile = walkingProfile ?? OsmProfiles.Pedestrian;
            _routerDb = new RouterDb();
            _routerDb.DataProvider = new DataProvider(_routerDb, baseTilesUrl);
        }

        public uint TimeBetween((double latitude, double longitude) from, IStop to)
        {
            var latE = (float) to.Latitude;
            var lonE = (float) to.Longitude;

            var lat = (float) from.latitude;
            var lon = (float) from.longitude;

            var startPoint = _routerDb.Snap(lon, lat);
            var endPoint = _routerDb.Snap(lonE, latE);

            if (startPoint.IsError || endPoint.IsError)
            {
                return uint.MaxValue;
            }

            var route = _routerDb.Calculate(_profile, startPoint.Value, endPoint.Value);

            if (route.IsError)
            {
                return uint.MaxValue;
            }

            return (uint) route.Value.TotalTime;
        }

        public Dictionary<LocationId, uint> TimesBetween(IStopsReader reader, (double latitude, double longitude) from, IEnumerable<IStop> to)
        {
            var result = new Dictionary<LocationId, uint>();
            
            foreach (var stop in to)
            {
                result[stop.Id] = TimeBetween(from, stop);
            }

            return result;
        }

        public float Range()
        {
            return _searchDistance;
        }
    }
}