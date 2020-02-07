using System;
using System.Collections.Generic;
using System.Linq;
using GeoAPI.Geometries;
using Itinero.Transit.Algorithms.Mergers;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.VectorTiles;

namespace Itinero.Transit.IO.VectorTiles
{
    public static class TransitDbToVectorTileExtensions
    {
        public static (VectorTileTree, BBox bbox, string sources) CalculateVectorTileTree(
            this IEnumerable<TransitDbSnapShot> tdbs, uint minZoom, uint maxZoom)
        {
            var bbox = new BBox();
            var sources = string.Empty;

            IEnumerable<(IFeature feature, int zoom, string layerName)> ConfigureFeature(IFeature feature)
            {
                for (var z = minZoom; z <= maxZoom; z++)
                {
                    switch (feature.Geometry)
                    {
                        case Point _:
                            yield return (feature, (int) z, "stops");
                            break;
                        case LineString _:
                            yield return (feature, (int) z, "routes");
                            break;
                        default:
                            throw new Exception("Unknown feature type");
                    }
                }
            }
            
            var vectorTileTree = new VectorTileTree();
            
            foreach (var tdb in tdbs)
            {
                vectorTileTree.Add(tdb.ToFeatures(bbox), ConfigureFeature);
                
                var source = tdb.GetAttribute("name", tdb.GlobalId);
                if (string.IsNullOrEmpty(source))
                {
                    sources = source;
                }
                else
                {
                    sources = " - " + source;
                }
            }

            return (vectorTileTree, bbox, sources);
        }

        private static IEnumerable<IFeature> ToFeatures(this TransitDbSnapShot transitDbSnapShot, BBox bbox)
        {
            var connectionFeatures = transitDbSnapShot.ToConnectionFeatures(out var tripsPerStop);
            foreach (var feature in connectionFeatures)
            {
                yield return feature;
            }

            foreach (var feature in transitDbSnapShot.ToStopFeatures(tripsPerStop, bbox))
            {
                yield return feature;
            }
        }

        private static IEnumerable<IFeature> ToConnectionFeatures(this TransitDbSnapShot transitDbSnapShot,
            out Dictionary<string, (HashSet<(string routeId, string routeType, string operatorId)> routes, int departures, int arrivals)> stopInfos)
        {
            stopInfos = new Dictionary<string, (HashSet<(string routeId, string routeType, string operatorId)> routes, int departures, int arrivals)>();
            var features = new Dictionary<(StopId stop1, StopId stop2), (Feature feature, int routes, int routeTypes, int operators)>();

            foreach (var connection in transitDbSnapShot.ConnectionsDb)
            {
                // create new feature if the stop combination doesn't exist yet.
                (StopId stop1, StopId stop2) key = (connection.DepartureStop, connection.ArrivalStop);
                string stop1GlobalId;
                string stop2GlobalId;
                if (!features.TryGetValue(key, out var feature))
                {
                    var stop1 = transitDbSnapShot.StopsDb.Get(key.stop1);
                    var stop2 = transitDbSnapShot.StopsDb.Get(key.stop2);
                    
                    feature = (new Feature(new LineString(new []
                    {
                        new Coordinate(stop1.Longitude, stop1.Latitude), 
                        new Coordinate(stop2.Longitude, stop2.Latitude), 
                    }), new AttributesTable()), 0, 0, 0);

                    feature.feature.Attributes.AddAttribute("stop_id_departure", stop1.GlobalId);
                    feature.feature.Attributes.AddAttribute("stop_id_arrival", stop2.GlobalId);
                    stop1GlobalId = stop1.GlobalId;
                    stop2GlobalId = stop2.GlobalId;
                    
                    features[key] = feature;
                }
                else
                {
                    stop1GlobalId = feature.feature.Attributes["stop_id_departure"] as string ?? string.Empty;
                    stop2GlobalId = feature.feature.Attributes["stop_id_arrival"] as string ?? string.Empty;
                }
                
                // get trip.
                var trip = transitDbSnapShot.TripsDb.Get(connection.TripId);
                
                // determine if operator is already there.
                Operator op = null;
                if (trip.Operator.DatabaseId != OperatorId.Invalid.DatabaseId ||
                    trip.Operator.LocalId != OperatorId.Invalid.LocalId)
                {
                    op = transitDbSnapShot.OperatorDb.Get(trip.Operator);

                    if (!feature.feature.Attributes.Exists($"operator_{op.GlobalId}"))
                    {
                        // add operator details.
                        feature.feature.Attributes.AddAttribute($"operator_{op.GlobalId}", "true");
                        feature.operators += 1;
                    }
                }

                // determine if route_type is already there.
                if (trip.TryGetAttribute("route_type", out var newRouteType))
                {
                    if (!feature.feature.Attributes.Exists($"route_type_{newRouteType}"))
                    {
                        // add route type.
                        feature.feature.Attributes.AddAttribute($"route_type_{newRouteType}", "true");
                        feature.routeTypes += 1;
                    }
                }
                
                // determine if route is already there.
                if (trip.TryGetAttribute("route_id", out var newRouteId))
                {   
                    if (!feature.feature.Attributes.Exists($"route_{newRouteId}"))
                    {
                        // add route.
                        feature.routes += 1;
                        feature.feature.Attributes.AddAttribute($"route_{newRouteId}",
                            Data.Route.FromTrip(trip).ToJson());

                        HashSet<(string routeId, string routeType, string operatorId)> routesList;
                        if (!stopInfos.TryGetValue(stop1GlobalId, out var stopInfo))
                        {
                            routesList = new HashSet<(string routeId, string routeType, string operatorId)>();
                            stopInfos[stop1GlobalId] = (routesList, 1, 0);
                        }
                        else
                        {
                            routesList = stopInfo.routes;
                            stopInfos[stop1GlobalId] = (routesList, 
                                stopInfo.departures + 1, stopInfo.arrivals);
                        }

                        routesList.Add((newRouteId, newRouteType, op?.GlobalId));
                    
                        if (!stopInfos.TryGetValue(stop2GlobalId, out stopInfo))
                        {
                            routesList = new HashSet<(string routeId, string routeType, string operatorId)>();
                            stopInfos[stop2GlobalId] = (routesList, 0, 1);
                        }
                        else
                        {
                            routesList = stopInfo.routes;
                            stopInfos[stop2GlobalId] = (routesList, 
                                stopInfo.departures, stopInfo.arrivals + 1);
                        }

                        routesList.Add((newRouteId, newRouteType, op?.GlobalId));
                    }
                }

                features[key] = feature;
            }

            return features.Values.Select(x =>
            {
                var (feature, routes, routeTypes, operators) = x;
                feature.Attributes.AddAttribute("route_count", routes);
                feature.Attributes.AddAttribute("route_type_count", routeTypes);
                feature.Attributes.AddAttribute("operator_count", operators);
                return feature;
            });
        }

        private static IEnumerable<IFeature> ToStopFeatures(this TransitDbSnapShot transitDbSnapShot,
            IReadOnlyDictionary<string, (HashSet<(string routeId, string routeType, string operatorId)> routes, int departures, int arrivals)> routesPerStops, BBox bbox)
        {
            foreach (var stop in transitDbSnapShot.StopsDb)
            {
                var feature = new Feature(new Point(stop.Longitude, stop.Latitude), 
                    new AttributesTable());
                bbox.AddCoordinate((stop.Longitude, stop.Latitude));
                
                feature.Attributes.AddAttribute("id", stop.GlobalId);

                foreach (var attribute in stop.Attributes)
                {
                    feature.Attributes.AddAttribute(attribute.Key, attribute.Value);
                }

                if (!routesPerStops.TryGetValue(stop.GlobalId, out var tripInfo)) continue;

                var routes = tripInfo.routes;
                var o = 0;
                foreach (var (routeId, routeType, operatorId) in routes)
                {
                    if (feature.Attributes.Exists($"route_{routeId}")) continue;
                    feature.Attributes.AddAttribute($"route_{routeId}", "true");

                    if (!feature.Attributes.Exists($"route_type_{routeType}"))
                    {
                        feature.Attributes.AddAttribute($"route_type_{routeType}", "true");
                    }

                    if (feature.Attributes.Exists($"operator_{operatorId}")) continue;
                    feature.Attributes.AddAttribute($"operator_{operatorId}", "true");
                    o++;
                }
                
                feature.Attributes.AddAttribute("route_count", routes.Count);
                feature.Attributes.AddAttribute("arrivals", tripInfo.arrivals);
                feature.Attributes.AddAttribute("departures", tripInfo.departures);
                feature.Attributes.AddAttribute("movements", tripInfo.departures + tripInfo.arrivals);
                feature.Attributes.AddAttribute("operator_count", o);

                yield return feature;
            }
        }
    }
}