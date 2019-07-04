using System;
using System.Collections.Generic;
using Itinero.IO.Osm.Tiles;
using Itinero.LocalGeo;
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
        ///  <param name="profile">The vehicle profile, default is pedestrian.</param>
        ///  <param name="baseTilesUrl">The base tile url.</param>
        public OsmTransferGenerator(
            float searchDistance = 1000,
            Profile profile = null,
            string baseTilesUrl = "https://tiles.openplanner.team/planet")
        {
            _searchDistance = searchDistance;
            _profile = profile ?? OsmProfiles.Pedestrian;
            _routerDb = new RouterDb();
            _routerDb.DataProvider = new DataProvider(_routerDb);
        }

        public uint TimeBetween(IStop from, IStop to)
        {
            var distance =
                Coordinate.DistanceEstimateInMeter(@from.Longitude, @from.Latitude, to.Longitude, to.Latitude);
            // Small patch for small distances...
            if (distance < 20)
            {
                return 0;
            }
            if (distance > 2500)
            {
                return uint.MaxValue;
            }

            var route = CreateRoute(from, to, out bool isEmpty);
            if (isEmpty)
            {
                return 0;
            }

            if (route == null)
            {
                return uint.MaxValue;
            }

            return (uint) route.TotalTime;

        }

        // ReSharper disable once MemberCanBePrivate.Global
        public Route CreateRoute(IStop from, IStop to, out bool isEmpty)
        {
            var startPoint = _routerDb.Snap(
                @from.Longitude,  @from.Latitude, profile: _profile);
            var endPoint = _routerDb.Snap(to.Longitude,  to.Latitude, profile: _profile);
            isEmpty = false;
            if (startPoint.IsError || endPoint.IsError)
            {
                Log.Information("Could not snap to either startPoint or endPoint");
                return null;
            }

            if (startPoint.Value.EdgeId == endPoint.Value.EdgeId &&
                startPoint.Value.Offset == endPoint.Value.Offset)
            {
                isEmpty = true;
                return null;
            }
            
            try
            {
                var route = _routerDb.Calculate(_profile, startPoint.Value, endPoint.Value);

                if (route.IsError)
                {
                    Log.Warning("Could not calculate route: isError ");
                    return null;
                }

                if (route.Value.TotalDistance > _searchDistance)
                {
                    // The total walking time exceeds the time that the traveller wants to walk between two stops
                    // We return MaxValue
                    // This can happen if a stop is in range crows-flight, but needs a detour to reach via the actual road network
                    return null;
                }

                return route.Value;
            }
            catch (Exception e)
            {
                Log.Error(
                    $"Could not calculate route from {from} to ({(float) to.Latitude},{(float) to.Longitude}) with itinero2.0: {e}");
                return null;
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