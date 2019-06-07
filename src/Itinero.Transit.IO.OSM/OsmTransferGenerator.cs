using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Profiles;
using Itinero.Transit.Data;
using Itinero.Transit.OtherMode;
using Vehicle = Itinero.Osm.Vehicles.Vehicle;

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
        private readonly Router _router;
        private readonly Profile _walkingProfile;

        private const float _searchDistance = 50f;


        // When  router db is loaded, it is saved into this dict to avoid reloading it
        private static readonly Dictionary<string, Router> _knownRouters
            = new Dictionary<string, Router>();


        ///  <summary>
        ///  Generate a new transfer generator, which takes into account
        ///  the time needed to transfer, walk, ...
        /// 
        ///  Footpaths are generated using an Osm-based router database
        ///  </summary>
        ///  <param name="routerdbPath">To create paths</param>
        ///  <param name="walkingProfile">How does the user transport himself over the OSM graph? Default is pedestrian</param>
        public OsmTransferGenerator(string routerdbPath,
            Profile walkingProfile = null)
        {
            _walkingProfile = walkingProfile ?? Vehicle.Pedestrian.Fastest();
            routerdbPath = Path.GetFullPath(routerdbPath);
            if (!_knownRouters.ContainsKey(routerdbPath))
            {
                using (var fs = new FileStream(routerdbPath, FileMode.Open, FileAccess.Read))
                {
                    var routerDb = RouterDb.Deserialize(fs);
                    if (routerDb == null)
                    {
                        throw new NullReferenceException("Could not load the routerDb");
                    }

                    _knownRouters[routerdbPath] = new Router(routerDb);
                }
            }

            _router = _knownRouters[routerdbPath];
        }


        public uint TimeBetween((double latitude, double longitude) from, IStop to)
        {
           
            var latE = (float) to.Latitude;
            var lonE = (float) to.Longitude;

            var lat = (float) from.latitude;
            var lon = (float) from.longitude;

            // ReSharper disable once RedundantArgumentDefaultValue
            var startPoint = _router.TryResolve(_walkingProfile, lat, lon, _searchDistance);
            // ReSharper disable once RedundantArgumentDefaultValue
            var endPoint = _router.TryResolve(_walkingProfile, latE, lonE, _searchDistance);

            if (startPoint.IsError || endPoint.IsError)
            {
                return uint.MaxValue;
            }

            var route = _router.TryCalculate(_walkingProfile, startPoint.Value, endPoint.Value);

            if (route.IsError)
            {
                return uint.MaxValue;
            }

            return (uint) route.Value.TotalTime;
        }

        public Dictionary<LocationId, uint> TimesBetween(IStopsReader reader, (double latitude, double longitude) from, IEnumerable<IStop> to)
        {
            foreach (var stop in to)
            {
                var lat = stop.Latitude;
                var lon = stop.Longitude;
            }
            
            throw new NotImplementedException();
        }

        public float Range()
        {
            return _searchDistance;
        }
    }
}