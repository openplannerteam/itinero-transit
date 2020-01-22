using System.Collections.Generic;
using GeoAPI.Geometries;
using Itinero.Transit.Data.Core;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Itinero.Transit.IO.VectorTiles
{
    internal class StopFeature : IFeature
    {
        public StopFeature(Stop stop, string operatorUrl, List<string> stops2Route)
        {
            Attributes = new AttributesTable();
            Attributes.AddAttribute("id", stop.GlobalId);
            Attributes.AddAttribute("agency:url", operatorUrl);

            foreach (var kv in stop.Attributes)
            {
                if (string.IsNullOrEmpty(kv.Value))
                {
                    continue;
                }

                Attributes.AddAttribute(kv.Key, kv.Value);
            }

            for (var index = 0; index < stops2Route.Count; index++)
            {
                var routeId = stops2Route[index];
                Attributes.AddAttribute("route" + index,
                    routeId); // We use this as to reuse strings as much as possible
            }

            Geometry = new Point(new Coordinate(stop.Longitude, stop.Latitude));
            BoundingBox = Geometry.EnvelopeInternal;
        }

        public IAttributesTable Attributes { get; set; }
        public IGeometry Geometry { get; set; }
        public Envelope BoundingBox { get; set; }
    }
}