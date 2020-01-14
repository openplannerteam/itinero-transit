using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GeoAPI.Geometries;
using Itinero.Transit.Algorithms.Mergers;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.VectorTiles;
using NetTopologySuite.IO.VectorTiles.Mapbox;

namespace Itinero.Transit.Processor.Switch
{
    class SwitchCreateVectorTiles : DocumentedSwitch, ITransitDbSink
    {
        private static readonly string[] _names = {"--create-vector-tiles", "--generate-vector-tiles", "--vt"};

        private static string About = "Creates a vector tile representation of the loaded transitDb";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.opt("directory", "The directory to write the data to")
                        .SetDefault("vector-tiles")
                };

        private const bool IsStable = true;


        public SwitchCreateVectorTiles
            () :
            base(_names, About, _extraParams, IsStable)
        {
        }

        public void Use(Dictionary<string, string> arguments, TransitDb tdb)
        {
            var writeTo = arguments["directory"];
            var latest = tdb.Latest;
            var connections = latest.ConnectionsDb;

            var minZoom = 1;
            var maxZoom = 25;

            if (File.Exists(writeTo))
            {
                throw new ArgumentException("The target directory " + writeTo + " already exists, but it is a file");
            }

            if (!Directory.Exists(writeTo))
            {
                Directory.CreateDirectory(writeTo);
            }


            var features = new FeatureCollection();

            var routes = new RouteMerger(connections);
            var routeId = (uint) 0;
            
            var stops2Routes = new Dictionary<string, List<string>>();
            foreach (var (route, trips) in routes.GetRouteToTrips())
            {
                var feature = new RouteFeature(latest, route, routeId,
                    latest.TripsDb.GetAll(trips), tdb.GlobalId);
                features.Add(feature);


                foreach (var stopId in route)
                {
                    var stop = latest.StopsDb.Get(stopId);
                    if (!stops2Routes.ContainsKey(stop.GlobalId))
                    {
                        stops2Routes[stop.GlobalId] = new List<string>();
                    }
                    stops2Routes[stop.GlobalId] .Add(""+routeId);
                }
                
                routeId++;
            }


            var stops = tdb.Latest.StopsDb;
            var minLat = double.MaxValue;
            var minLon = double.MaxValue;
            var maxLat = double.MinValue;
            var maxLon = double.MinValue;
            var empty = new List<string>();
            foreach (var stop in stops)
            {
                features.Add(
                    new StopFeature(stop, tdb.GlobalId, 
                        stops2Routes.GetValueOrDefault(stop.GlobalId, empty)));

                minLat = Math.Min(stop.Latitude, minLat);
                minLon = Math.Min(stop.Longitude, minLon);
                maxLat = Math.Max(stop.Latitude, maxLat);
                maxLon = Math.Max(stop.Longitude, maxLon);
            }


            IEnumerable<(IFeature feature, int zoom, string layerName)> ConfigureFeature(IFeature feature)
            {
                for (var z = minZoom; z <= maxZoom; z++)
                {
                    switch (feature)
                    {
                        case StopFeature _:
                            yield return (feature, z, "stops");
                            break;
                        case RouteFeature _:
                            yield return (feature, z, "routes");
                            break;
                        default:
                            throw new Exception("Unknown feature type");
                    }
                }
            }

            var tree = new VectorTileTree()
                {{features, ConfigureFeature}};


            tree.Write(writeTo);

            var bounds =
                $"[{minLon}, {minLat}, {maxLon}, {maxLat}]";

            var mvtFileContents = GenerateMvtJson(
                "Public transport data", "Information about a public transport operator",
                $"Data from {tdb.GetAttribute("name", "")} - generated with Itinero.Transit",
                "https://anyways.eu", "",
                bounds, minZoom, maxZoom,
                new[]
                {
                    ("stops", "The location of all the stops"),
                    ("routes", "All the routes (which can have multiple services a day)")
                });

            File.WriteAllText(Path.Combine(writeTo, "mvt.json"), mvtFileContents);

            Console.WriteLine("Vector tile exportation complete");
        }

        /// <summary>
        /// Generates the accompanying manifest 'mvt.json'
        /// </summary>
        /// <param name="name">The name of the collection</param>
        /// <param name="description">The description of the layer</param>
        /// <param name="attribution">The attribution of the entire layer</param>
        /// <param name="host">E.g. 'https://anyways.eu'</param>
        /// <param name="endpoint">E.g. 'vector-tiles/public-transport/'</param>
        /// <param name="bounds">A json-list of bounds, format "[minLon, minLat, maxLon, MaxLat]", e.g. "[2.5, 49.5, 6.4, 51.5]"</param>
        /// <param name="minZoom">The minimal zoom level generated</param>
        /// <param name="maxZoom">The maximum zoom level generated</param>
        /// <param name="layerInfo">Info on the other layers</param>
        /// <returns></returns>
        private static string GenerateMvtJson(
            string name, string description, string attribution,
            string host, string endpoint,
            string bounds, int minZoom, int maxZoom,
            IEnumerable<(string name, string description)> layerInfo)
        {
            var layerInfoString = string.Join(",",
                layerInfo.Select(l => "    {\n" +
                                      $"        \"minzoom\": {minZoom},\n" +
                                      $"        \"maxzoom\": {maxZoom},\n" +
                                      $"        \"id\": \"{l.name}\",\n" +
                                      $"        \"description\": \"{l.description}\"\n" +
                                      "    }"));
            var mvt =
                "{\n" +
                "    \"tiles\": [\n" +
                $"    \"{host}/{endpoint}/" + "{z}/{x}/{y}.mvt\"\n" +
                "        ],\n" +
                $"    \"minzoom\": {minZoom},\n" +
                $"    \"maxzoom\": {maxZoom},\n" +
                $"    \"bounds\": {bounds},\n" +
                $"    \"name\": \"{name}\",\n" +
                $"    \"description\": \"{description}\",\n" +
                $"    \"attribution\": \"{attribution}\",\n" +
                "    \"format\": \"pbf\",\n" +
                $"    \"id\": \"{name}\",\n" +
                $"    \"basename\": \"{name}\",\n" +
                "    \"vector_layers\": [\n" +
                layerInfoString +
                "    ],\n" +
                "    \"version\": \"1.0\",\n" +
                "    \"tilejson\": \"2.0.0\"\n" +
                "}\n";
            return mvt;
        }
    }

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
            Attributes.AddAttribute("id",  "" + routeId);

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

    internal class StopFeature : IFeature
    {
        public StopFeature(Stop stop, string operatorUrl, List<string> stops2Route)
        {
            Attributes = new AttributesTable();
            Attributes.AddAttribute("id", stop.GlobalId);
            Attributes.AddAttribute("operator", operatorUrl);

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
                Attributes.AddAttribute("route" + index,routeId); // We use this as to reuse strings as much as possible
            }

            Geometry = new Point(new Coordinate(stop.Longitude, stop.Latitude));
            BoundingBox = Geometry.EnvelopeInternal;
        }

        public IAttributesTable Attributes { get; set; }
        public IGeometry Geometry { get; set; }
        public Envelope BoundingBox { get; set; }
    }
}