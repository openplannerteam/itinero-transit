using System;
using System.Collections.Generic;
using Itinero.IO.Osm.Tiles;
using Itinero.Profiles;
using Itinero.Profiles.Lua.Osm;
using Itinero.Transit.Data;
using Itinero.Transit.Logging;
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

        private readonly float _searchDistance;

        ///  <summary>
        ///  Generate a new transfer generator, which takes into account
        ///  the time needed to transfer, walk, ...
        /// 
        ///  Footpaths are generated using an Osm-based router database
        ///  </summary>
        ///  <param name="searchDistance">The maximum distance that the traveller takes this route</param>
        ///  <param name="walkingProfile">The vehicle profile, default is pedestrian.</param>
        ///  <param name="baseTilesUrl">The base tile url.</param>
        public OsmTransferGenerator(
            float searchDistance = 100,
            Profile walkingProfile = null,
            string baseTilesUrl = "https://tiles.openplanner.team/planet")
        {
            _searchDistance = searchDistance;
            _profile = walkingProfile ?? OsmProfiles.Pedestrian;
            _routerDb = new RouterDb();
            _routerDb.DataProvider = new DataProvider(_routerDb, baseTilesUrl);
        }

        public uint TimeBetween(IStop from, IStop to)
        {
            var startPoint = _routerDb.Snap((float) @from.Longitude, (float) @from.Latitude);
            var endPoint = _routerDb.Snap((float) to.Longitude, (float) to.Latitude);

            if (startPoint.IsError || endPoint.IsError)
            {
                return uint.MaxValue;
            }

            try
            {
                var route = _routerDb.Calculate(_profile, startPoint.Value, endPoint.Value);

                if (route.IsError)
                {
                    return uint.MaxValue;
                }

                if (route.Value.TotalDistance > _searchDistance)
                {
                    // The total walking time exceeds the time that the traveller wants to walk between two stops
                    // We return MaxValue
                    // This can happen if a stop is in range crows-flight, but needs a detour to reach via the actual road network
                    return uint.MaxValue;
                }

                return (uint) route.Value.TotalTime;
            }
            catch (Exception e)
            {
                Log.Error(
                    $"Could not calculate route from {from} to ({(float) to.Latitude},{(float) to.Longitude}) with itinero2.0: {e}");
                return uint.MaxValue;
            }
        }

        public Dictionary<LocationId, uint> TimesBetween(IStop from,
            IEnumerable<IStop> to)
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
        
        public string OtherModeIdentifier()
        {
            return $"https://openplanner.team/itinero-transit/walks/osm&maxDistance={_searchDistance}&profile={_profile.Name}";
        }
    }
}