using System;
using System.IO;
using Itinero.Exceptions;
using Itinero.Profiles;
using static Itinero.Osm.Vehicles.Vehicle;

namespace Itinero.Transit
{
    /// <inheritdoc />
    /// <summary>
    /// The transfer generator has the responsibility of creating
    /// transfers between multiple locations, possibly intermodal.
    /// If the departure and arrival location are the same, an internal
    /// transfer is generate.
    /// If not, the OpenStreetMap database is queried to generate a path between them.
    /// </summary>
    public class TransferGenerator : IFootpathTransferGenerator
    {
        private readonly Router _router;
        private readonly Profile _walkingProfile;
        private readonly float _speed;
        private const int SearchDistance = 50;

        private readonly int _internalTransferTime;

        /// <summary>
        /// Generate a new transfer generator, which takes into account
        /// the time needed to transfer, walk, ...
        /// </summary>
        /// <param name="routerdbPath">To create paths</param>
        /// <param name="speed">The walking speed (in meter/second)</param>
        /// <param name="internalTransferTime">How many seconds does it take to go from one platform to another. Default is 180s</param>
        /// <param name="walkingProfile">How does the user transport himself over the OSM graph? Default is pedestrian</param>
        public TransferGenerator(string routerdbPath, float speed = 1.3f, int internalTransferTime = 180,
            Profile walkingProfile = null)
        {
            _speed = speed;
            _internalTransferTime = internalTransferTime;
            if (internalTransferTime < 0)
            {
                throw new ArgumentException("The internal transfer time should be >= 0");
            }

            _walkingProfile = walkingProfile ?? Pedestrian.Fastest();
            using (var fs = new FileStream(routerdbPath, FileMode.Open, FileAccess.Read))
            {
                var routerDb = RouterDb.Deserialize(fs);
                _router = new Router(routerDb);
            }
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