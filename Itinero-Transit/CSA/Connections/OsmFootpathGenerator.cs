using System;
using System.Collections.Generic;
using System.IO;
using Itinero;
using Itinero_Transit.CSA.ConnectionProviders;
using static Itinero.Osm.Vehicles.Vehicle;

namespace Itinero_Transit.CSA.Connections
{
    public class OsmFootpathGenerator : IFootpathTransferGenerator
    {
        private readonly ILocationProvider _locationDecoder;
        private readonly Router _router;


        public OsmFootpathGenerator(ILocationProvider locationDecoder, string routerdbPath)
        {
            _locationDecoder = locationDecoder;
            using (var fs = new FileStream(routerdbPath, FileMode.Open, FileAccess.Read))
            {
                var _routerDb = RouterDb.Deserialize(fs);
                _router = new Router(_routerDb);
            }
        }

        public IConnection GenerateFootPaths(DateTime departureTime, Uri from, Uri to)
        {
            var start = _locationDecoder.GetCoordinateFor(from);
            var end = _locationDecoder.GetCoordinateFor(to);
            var startPoint = _router.Resolve(Pedestrian.Shortest(), start.Lat, start.Lon, searchDistanceInMeter: 50f);
            var endPoint = _router.Resolve(Pedestrian.Shortest(), end.Lat, end.Lon, 50f);
            var route = _router.Calculate(Pedestrian.Shortest(), startPoint, endPoint);
            return new WalkingConnection(route, from, to, departureTime);
        }
    }
}