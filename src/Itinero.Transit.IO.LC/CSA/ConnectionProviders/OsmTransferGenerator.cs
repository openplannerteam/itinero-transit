using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Profiles;
using static Itinero.Osm.Vehicles.Vehicle;

namespace Itinero.IO.LC
{
    /// <inheritdoc />
    /// <summary>
    /// The transfer generator has the responsibility of creating
    /// transfers between multiple locations, possibly intermodal.
    /// If the departure and arrival location are the same, an internal
    /// transfer is generate.
    /// If not, the OpenStreetMap database is queried to generate a path between them.
    /// </summary>
    public class OsmTransferGenerator : IFootpathTransferGenerator
    {
        private readonly Router _router;
        private readonly Profile _walkingProfile;
        private readonly float _speed;
        private const int SearchDistance = 50;

        // When a router db is loaded, it is saved into this dict to avoid reloading it
        private static readonly Dictionary<string, Router> KnownRouters
            = new Dictionary<string, Router>();

        private readonly int _internalTransferTime;

        /// <summary>
        /// Generate a new transfer generator, which takes into account
        /// the time needed to transfer, walk, ...
        ///
        /// Footpaths are generated using an Osm-based router database
        /// </summary>
        /// <param name="routerdbPath">To create paths</param>
        /// <param name="speed">The walking speed (in meter/second)</param>
        /// <param name="internalTransferTime">How many seconds does it take to go from one platform to another. Default is 180s</param>
        /// <param name="walkingProfile">How does the user transport himself over the OSM graph? Default is pedestrian</param>
        public OsmTransferGenerator(string routerdbPath, float speed = 1.3f, int internalTransferTime = 180,
            Profile walkingProfile = null)
        {
            _speed = speed;
            _internalTransferTime = internalTransferTime;
            if (internalTransferTime < 0)
            {
                throw new ArgumentException("The internal transfer time should be >= 0");
            }

            _walkingProfile = walkingProfile ?? Pedestrian.Fastest();
            routerdbPath = Path.GetFullPath(routerdbPath);
            if (!KnownRouters.ContainsKey(routerdbPath))
            {
                using (var fs = new FileStream(routerdbPath, FileMode.Open, FileAccess.Read))
                {
                    var routerDb = RouterDb.Deserialize(fs);
                    if (routerDb == null)
                    {
                        throw new NullReferenceException("Could not load the routerDb");
                    }

                    KnownRouters[routerdbPath] = new Router(routerDb);
                }
            }

            _router = KnownRouters[routerdbPath];
        }

        public IContinuousConnection GenerateFootPaths(DateTime departureTime, Location from, Location to)
        {
            if (from.Uri.Equals(to.Uri))
            {
                // Special case: departure location and arrival location are the same
                // This often represents a transfer within the same station, where platforms are not given

                return new InternalTransfer(from.Uri, departureTime, departureTime.AddSeconds(_internalTransferTime));
            }

            try
            {
                var startPoint = _router.Resolve(_walkingProfile, from.Lat, from.Lon, SearchDistance);
                var endPoint = _router.Resolve(_walkingProfile, to.Lat, to.Lon, SearchDistance);
                var route = _router.Calculate(_walkingProfile, startPoint, endPoint);
                return new WalkingConnection(route, from.Uri, to.Uri, departureTime, _speed);
            }
            catch
            {
                return null;
            }
        }
    }
}