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
            out Dictionary<string, (HashSet<(string tripId, string operatorId)> trips, int departures, int arrivals)> stopInfos)
        {
            stopInfos = new Dictionary<string, (HashSet<(string tripId, string operatorId)> trips, int departures, int arrivals)>();
            var features = new Dictionary<(StopId stop1, StopId stop2), (Feature feature, int trips, int routeTypes, int operators)>();

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
                var op = 0;
                Operator oper = null;
                if (trip.Operator.DatabaseId != OperatorId.Invalid.DatabaseId ||
                    trip.Operator.LocalId != OperatorId.Invalid.LocalId)
                {
                    oper = transitDbSnapShot.OperatorDb.Get(trip.Operator);
                    var operatorFound = false;
                    for (; op < feature.operators; op++)
                    {
                        if (!(feature.feature.Attributes[$"operator_{op:00000}_id"] is string operatorId)) continue;
                        if (oper.GlobalId != operatorId) continue;

                        operatorFound = true;
                        break;
                    }

                    if (!operatorFound)
                    {
                        // add operator details.
                        feature.feature.Attributes.AddAttribute($"operator_{op:00000}_id", oper.GlobalId);
                        foreach (var tripAttribute in trip.Attributes)
                        {
                            feature.feature.Attributes.AddAttribute($"operator_{op:00000}_{tripAttribute.Key}",
                                tripAttribute.Value);
                        }
                        feature.operators += 1;
                    }
                }
                
                // determine if route_type is already there.
                var rt = 0;
                if (trip.TryGetAttribute("route_type", out var newRouteType))
                {
                    var routeTypeFound = false;
                    for (; rt < feature.routeTypes; rt++)
                    {
                        var routeType = feature.feature.Attributes[$"route_type_{rt:00000}"];
                        if (!(routeType is string routeTypeString) || newRouteType != routeTypeString) continue;

                        routeTypeFound = true;
                        break;
                    }

                    if (!routeTypeFound)
                    {
                        // add route type.
                        feature.feature.Attributes.AddAttribute($"route_type_{rt:00000}", newRouteType);
                        feature.routeTypes += 1;
                    }
                }

                // determine if trip is already here.
                var tripFound = false;
                var t = 0;
                for (; t < feature.trips; t++)
                {
                    var tripId = feature.feature.Attributes[$"trip_{t:00000}_id"];
                    if (!(tripId is string tripIdString) || tripIdString != trip.GlobalId) continue;
                    
                    tripFound = true;
                    break;
                }

                if (!tripFound)
                {
                    // add trip.
                    feature.feature.Attributes.AddAttribute($"trip_{t:00000}_id", trip.GlobalId);
                    foreach (var tripAttribute in trip.Attributes)
                    {
                        feature.feature.Attributes.AddAttribute($"trip_{t:00000}_{tripAttribute.Key}",
                            tripAttribute.Value);
                    }
                    if (oper != null) feature.feature.Attributes.AddAttribute($"trip_{t:00000}_operator_id", oper.GlobalId);

                    HashSet<(string tripId, string operatorId)> tripsList;
                    if (!stopInfos.TryGetValue(stop1GlobalId, out var stopInfo))
                    {
                        tripsList = new HashSet<(string tripId, string operatorId)>();
                        stopInfos[stop1GlobalId] = (tripsList, 1, 0);
                    }
                    else
                    {
                        tripsList = stopInfo.trips;
                        stopInfos[stop1GlobalId] = (tripsList, 
                            stopInfo.departures + 1, stopInfo.arrivals);
                    }

                    tripsList.Add((trip.GlobalId, oper?.GlobalId));
                    
                    if (!stopInfos.TryGetValue(stop2GlobalId, out stopInfo))
                    {
                        tripsList = new HashSet<(string tripId, string operatorId)>();
                        stopInfos[stop2GlobalId] = (tripsList, 0, 1);
                    }
                    else
                    {
                        tripsList = stopInfo.trips;
                        stopInfos[stop2GlobalId] = (tripsList, 
                            stopInfo.departures, stopInfo.arrivals + 1);
                    }

                    tripsList.Add((trip.GlobalId, oper?.GlobalId));

                    feature.trips += 1;
                }

                features[key] = feature;
            }

            return features.Values.Select(x =>
            {
                var (feature, trips, routeTypes, operators) = x;
                feature.Attributes.AddAttribute("trip_count", trips);
                feature.Attributes.AddAttribute("route_type_count", routeTypes);
                feature.Attributes.AddAttribute("operator_count", operators);
                return feature;
            });
        }

        private static IEnumerable<IFeature> ToStopFeatures(this TransitDbSnapShot transitDbSnapShot,
            IReadOnlyDictionary<string, (HashSet<(string tripId, string operatorId)> trips, int departures, int arrivals)> tripsPerStop, BBox bbox)
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

                if (!tripsPerStop.TryGetValue(stop.GlobalId, out var tripInfo)) continue;

                var trips = tripInfo.trips;
                var t = 0;
                var o = 0;
                foreach (var (tripId, operatorId) in trips)
                {
                    feature.Attributes.AddAttribute($"trip_{t:00000}_id", tripId);

                    if (!string.IsNullOrEmpty(operatorId))
                    {
                        var operatorFound = false;
                        for (var i = 0; i < o; i++)
                        {
                            var existingOperatorId = feature.Attributes[$"operator_{i:00000}_id"] as string;
                            if (existingOperatorId != operatorId) continue;
                            
                            operatorFound = true;
                            break;
                        }

                        if (!operatorFound)
                        {
                            feature.Attributes.AddAttribute($"operator_{o:00000}_id", operatorId);
                            o++;
                        }
                    }

                    t++;
                }
                feature.Attributes.AddAttribute("trip_count", trips.Count);
                feature.Attributes.AddAttribute("arrivals", tripInfo.arrivals);
                feature.Attributes.AddAttribute("departures", tripInfo.departures);
                feature.Attributes.AddAttribute("movements", tripInfo.departures + tripInfo.arrivals);
                feature.Attributes.AddAttribute("operator_count", o);

                yield return feature;
            }
        }
    }
}