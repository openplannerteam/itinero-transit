using System;
using System.Collections.Generic;
using System.Linq;
using GeoAPI.Geometries;
using Itinero.Transit.Algorithms.Mergers;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Itinero.Transit.IO.VectorTiles
{
    internal class RouteFeature : IFeature
    {
        public IAttributesTable Attributes { get; set; }
        public IGeometry Geometry { get; set; }
        public Envelope BoundingBox { get; set; }

        public RouteFeature(TransitDbSnapShot tdb, Route route, uint routeId, IReadOnlyList<Trip> trips,
            string operatorUrl)
        {
            Attributes = new AttributesTable();
            Attributes.AddAttribute("operator", operatorUrl);
            Attributes.AddAttribute("id", "" + routeId);

            for (var i = 0; i < trips.Count; i++)
            {
                var trip = trips[i];

                Attributes.AddAttribute("trip" + i, trip.GlobalId);
                if (trip.TryGetAttribute("headsign", out var headsign))
                {
                    Attributes.AddAttribute($"trip{i}:headsign", headsign);
                }

                if (trip.TryGetAttribute("shortname", out var shortname))
                {
                    Attributes.AddAttribute($"trip{i}:shortname", shortname);
                }
            }


            var points = new List<Coordinate>();
            var allStops = tdb.StopsDb.GetAll(route.Reverse().ToList());
            var minLat = double.MaxValue;
            var minLon = double.MaxValue;
            var maxLat = double.MinValue;
            var maxLon = double.MinValue;
            for (var index = 0; index < allStops.Count; index++)
            {
                var stop = allStops[index];
                Attributes.AddAttribute("stop" + index, stop.GlobalId);

                points.Add(new Coordinate(stop.Longitude, stop.Latitude));

                minLat = Math.Min(stop.Latitude, minLat);
                minLon = Math.Min(stop.Longitude, minLon);
                maxLat = Math.Max(stop.Latitude, maxLat);
                maxLon = Math.Max(stop.Longitude, maxLon);
            }

            Geometry = new LineString(points.ToArray());
            BoundingBox = new Envelope(minLon, maxLon, minLat, maxLat);
        }
    }
}