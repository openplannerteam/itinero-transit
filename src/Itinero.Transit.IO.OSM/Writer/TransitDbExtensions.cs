using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using OsmSharp;
using OsmSharp.Tags;

namespace Itinero.Transit.IO.OSM.Writer
{
    public static class TransitDbExtensions
    {
        public static IEnumerable<OsmGeo> ToOsmStreamSource(this TransitDbSnapShot snapShot, 
            Func<OsmGeoType, long> osmIdGenerator = null)
        {
            var stops = snapShot.StopsDb;
            var stopsIndex = new Dictionary<string, Node>();

            foreach (var stop in stops)
            {
                var stopNode = new Node();
                stopNode.Id = osmIdGenerator(OsmGeoType.Node);
                stopNode.Latitude = stop.Latitude;
                stopNode.Longitude = stop.Longitude;

                var tagsCollection = new TagsCollection();
                tagsCollection.AddOrReplace("stop_id", stop.GlobalId);
                stopNode.Tags = tagsCollection;
                
                yield return stopNode;

                stopsIndex[stop.GlobalId] = stopNode;
            }

            var ways = snapShot.ToConnectionWays(stopsIndex, osmIdGenerator);
            foreach (var way in ways.Values)
            {
                yield return way;
            }

            var relations = snapShot.ToConnectionRelations(ways, osmIdGenerator);
            foreach (var relation in relations.Values)
            {
                yield return relation;
            }
        }

        private static Dictionary<(StopId stop1, StopId stop2), Way> ToConnectionWays(this TransitDbSnapShot transitDbSnapShot, 
            Dictionary<string, Node> stopNodes, Func<OsmGeoType, long> osmIdGenerator = null)
        {
            var ways = new Dictionary<(StopId stop1, StopId stop2), Way>();

            foreach (var connection in transitDbSnapShot.ConnectionsDb)
            {
                // create new feature if the stop combination doesn't exist yet.
                (StopId stop1, StopId stop2) key = (connection.DepartureStop, connection.ArrivalStop);
                string stop1GlobalId;
                string stop2GlobalId;
                if (!ways.TryGetValue(key, out var way))
                {
                    var stop1 = transitDbSnapShot.StopsDb.Get(key.stop1);
                    var stop2 = transitDbSnapShot.StopsDb.Get(key.stop2);

                    if (!stopNodes.TryGetValue(stop1.GlobalId, out var stopNode1) ||
                        !stopNodes.TryGetValue(stop2.GlobalId, out var stopNode2))
                    {
                        continue;
                    }
                    
                    way = new Way();
                    way.Id = osmIdGenerator(OsmGeoType.Way);
                    way.Nodes = new[]
                    {
                        stopNode1.Id.Value,
                        stopNode2.Id.Value
                    };
                    
                    var tagsCollection = new TagsCollection();
                    tagsCollection.AddOrReplace("stop_id_departure", stop1.GlobalId);
                    tagsCollection.AddOrReplace("stop_id_arrival", stop2.GlobalId);
                    way.Tags = tagsCollection;
                    
                    stop1GlobalId = stop1.GlobalId;
                    stop2GlobalId = stop2.GlobalId;
                    
                    ways[key] = way;
                }
                else
                {
//                    stop1GlobalId = feature.feature.Attributes["stop_id_departure"] as string ?? string.Empty;
//                    stop2GlobalId = feature.feature.Attributes["stop_id_arrival"] as string ?? string.Empty;
                }
            }

            return ways;
        }

        private static Dictionary<string, Relation> ToConnectionRelations(this TransitDbSnapShot transitDbSnapShot, 
            Dictionary<(StopId stop1, StopId stop2), Way> ways, Func<OsmGeoType, long> osmIdGenerator = null)
        {
            var relations = new Dictionary<string, Relation>();
            
            foreach (var connection in transitDbSnapShot.ConnectionsDb)
            {
                // create new feature if the stop combination doesn't exist yet.
                (StopId stop1, StopId stop2) key = (connection.DepartureStop, connection.ArrivalStop);
                if (!ways.TryGetValue(key, out var way))
                {
                    continue;
                }
                
                // get trip.
                var trip = transitDbSnapShot.TripsDb.Get(connection.TripId);
                
                // determine if route is already there.
                if (!trip.TryGetAttribute("route_id", out var newRouteId))
                {
                    continue;
                }

                if (!relations.TryGetValue(newRouteId, out var routeRelation))
                {
                    routeRelation = new Relation();
                    routeRelation.Id = osmIdGenerator(OsmGeoType.Relation);
                    routeRelation.Members = new RelationMember[0];
                    
                    var tags = new TagsCollection();
                    tags.AddOrReplace("type", "route");
                    if (trip.TryGetAttribute("route_type", out var routeType))
                    {
                        tags.AddOrReplace("route", routeType);
                    }
                    if (trip.TryGetAttribute("route_shortname", out var routeShortName))
                    {
                        tags.AddOrReplace("shortname", routeShortName);
                    }
                    if (trip.TryGetAttribute("route_longname", out var routeLongName))
                    {
                        tags.AddOrReplace("longname", routeLongName);
                    }

                    var op = transitDbSnapShot.OperatorDb.Get(trip.Operator);
                    if (op.TryGetAttribute("name", out var opName))
                    {
                        tags.AddOrReplace("operator", opName);
                    }
                    
                    routeRelation.Tags = tags;
                    
                    // TODO: add operator.

                    relations[newRouteId] = routeRelation;
                }
                
                // add stops.
                var members = new List<RelationMember>(routeRelation.Members);
                var stop1Found = false;
                var stop2Found = false;
                var wayFound = false;
                foreach (var member in routeRelation.Members)
                {
                    if (member.Type == OsmGeoType.Node &&
                        member.Id == way.Nodes[0])
                    {
                        stop1Found = true;
                    }
                    else if (member.Type == OsmGeoType.Node &&
                             member.Id == way.Nodes[1])
                    {
                        stop2Found = true;
                    }
                    else if (member.Type == OsmGeoType.Way &&
                             member.Id == way.Id)
                    {
                        wayFound = true;
                    }
                }

                if (!stop1Found)
                {
                    members.Add(new RelationMember()
                    {
                        Id = way.Nodes[0],
                        Role = "platform",
                        Type = OsmGeoType.Node
                    });
                }
                if (!stop2Found)
                {
                    members.Add(new RelationMember()
                    {
                        Id = way.Nodes[1],
                        Role = "platform",
                        Type = OsmGeoType.Node
                    });
                }

                if (!wayFound)
                {
                    members.Add(new RelationMember()
                    {
                        Id = way.Id.Value,
                        Type = OsmGeoType.Way
                    });
                }

                routeRelation.Members = members.ToArray();
            }

            return relations;
        }
    }
}