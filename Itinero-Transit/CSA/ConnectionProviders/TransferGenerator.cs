using System;
using System.IO;
using Itinero;
using Itinero.Profiles;
using Itinero_Transit.CSA.ConnectionProviders;
using static Itinero.Osm.Vehicles.Vehicle;

namespace Itinero_Transit.CSA.Connections
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
        private readonly ILocationProvider _locationDecoder;
        private readonly Router _router;
        private readonly Profile _walkingProfile;
        private readonly float _speed;
        private const int SearchDistance = 50;

        private readonly int _internalTransferTime;

        /// <summary>
        /// Generate a new transfer generator, which takes into account
        /// the time needed to transfer, walk, ...
        /// </summary>
        /// <param name="locationDecoder">To find coordinates of the IDS</param>
        /// <param name="routerdbPath">To create paths</param>
        /// <param name="speed">The walking speed (in meter/second)</param>
        /// <param name="internalTransferTime">How many seconds does it take to go from one platform to another. Default is 180s</param>
        /// <param name="walkingProfile">How does the user transport himself over the OSM graph? Default is pedestrian</param>
        public TransferGenerator(ILocationProvider locationDecoder,
            string routerdbPath, float speed=1.3f, int internalTransferTime = 180, Profile walkingProfile = null)
        {
            _locationDecoder = locationDecoder;
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

        /// <inheritdoc />
        /// <summary>
        /// Generate the footpaths that connect the given connections.
        /// Returns null if there is not enough time between them
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public IConnection CalculateInterConnection(IConnection from, IConnection to)
        {

            var footpath = GenerateFootPaths(from.ArrivalTime(), from.ArrivalLocation(), to.DepartureLocation());

            if (footpath.ArrivalTime() > to.DepartureTime())
            {
                // we can't make it in time to the connection where we are supposed to go
                return null;
            }

            return footpath;

        }

        public IConnection GenerateFootPaths(DateTime departureTime, Uri from, Uri to)
        {
            if (from.Equals(to))
            {
                // Special case: departure location and arrival location are the same
                // This often represents a transfer within the same station, where platforms are not given
                
                return new InternalTransfer(from, departureTime, departureTime.AddSeconds(_internalTransferTime));
                
            }


            var start = _locationDecoder.GetCoordinateFor(from);
            var end = _locationDecoder.GetCoordinateFor(to);
            var startPoint = _router.Resolve(_walkingProfile, start.Lat, start.Lon, SearchDistance);
            var endPoint = _router.Resolve(_walkingProfile, end.Lat, end.Lon, SearchDistance);
            var route = _router.Calculate(_walkingProfile, startPoint, endPoint);
            return new WalkingConnection(route, from, to, departureTime, _speed);
        }
    }
}