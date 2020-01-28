using System;
using System.Collections.Generic;
using Itinero.Transit.Algorithms.Mergers;
using Itinero.Transit.Data;
using NetTopologySuite.Features;
using NetTopologySuite.IO.VectorTiles;

namespace Itinero.Transit.IO.VectorTiles
{
    public static class TransitDbToVectorTileExtensions
    {
        public static (VectorTileTree, BBox bbox, string sources) CalculateVectorTileTree(
            this IEnumerable<TransitDbSnapShot> tdbs, uint minZoom, uint maxZoom)
        {
            var features = new FeatureCollection();
            var bbox = new BBox();
            var sources = "";

            foreach (var tdb in tdbs)
            {
                var stops2Routes = CalculateRoutes(features, tdb);
                var bboxTdb = AddStops(tdb, features, stops2Routes);
                bbox.AddBBox(bboxTdb);
                var source = tdb.GetAttribute("name", tdb.GlobalId);
                sources += source + ";";
            }

            IEnumerable<(IFeature feature, int zoom, string layerName)> ConfigureFeature(IFeature feature)
            {
                for (var z = minZoom; z <= maxZoom; z++)
                {
                    switch (feature)
                    {
                        case StopFeature _:
                            yield return (feature, (int) z, "stops");
                            break;
                        case RouteFeature _:
                            yield return (feature, (int) z, "routes");
                            break;
                        default:
                            throw new Exception("Unknown feature type");
                    }
                }
            }

            return (new VectorTileTree
            {
                {
                    features, ConfigureFeature
                }
            }, bbox, sources);
        }

        private static Dictionary<string, List<string>> CalculateRoutes(FeatureCollection addFeatures,
            TransitDbSnapShot tdb)
        {
            var connections = tdb.Connections;

            var routes = new RouteMerger(connections);
            var routeId = (uint) 0;

            var stops2Routes = new Dictionary<string, List<string>>();
            foreach (var kv in routes.GetRouteToTrips())
            {
                var route = kv.Key;
                var trips = kv.Value;
                
                var feature = new RouteFeature(tdb, route, routeId,
                    tdb.Trips.GetAll(trips), tdb.GlobalId);
                addFeatures.Add(feature);


                foreach (var stopId in route)
                {
                    var stop = tdb.Stops.Get(stopId);
                    if (!stops2Routes.ContainsKey(stop.GlobalId))
                    {
                        stops2Routes[stop.GlobalId] = new List<string>();
                    }

                    stops2Routes[stop.GlobalId].Add("" + routeId);
                }

                routeId++;
            }

            return stops2Routes;
        }


        private static BBox AddStops(TransitDbSnapShot tdb,
            FeatureCollection features, IReadOnlyDictionary<string, List<string>> stops2Routes)
        {
            var stops = tdb.Stops;

            var empty = new List<string>();
            var bbox = new BBox();
            foreach (var stop in stops)
            {
                stops2Routes.TryGetValue(stop.GlobalId, out var routes);
                routes = routes ?? empty;
                
                features.Add(
                    new StopFeature(stop, tdb.GlobalId,routes));

                bbox.AddCoordinate((stop.Longitude, stop.Latitude));
            }

            return bbox;
        }
    }
}