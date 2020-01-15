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

namespace Itinero.Transit.Processor.Switch.Write
{
    internal class WriteVectorTiles : DocumentedSwitch, IMultiTransitDbSink
    {
        private static readonly string[] _names = {"--write-vector-tiles", "--write-vt", "--vt"};

        private static string About = "Creates a vector tile representation of the loaded transitDb";


        private static readonly List<(List<string> args, bool isObligated, string comment, string defaultValue)>
            _extraParams =
                new List<(List<string> args, bool isObligated, string comment, string defaultValue)>
                {
                    SwitchesExtensions.opt("directory", "The directory to write the data to")
                        .SetDefault("vector-tiles"),
                    SwitchesExtensions.opt("minzoom", "The minimal zoom level that this vector tiles are generated for")
                        .SetDefault("3"),
                    SwitchesExtensions.opt("maxzoom", "The maximal zoom level that the vector tiles are generated for. Note: maxzoom should be pretty big, as lines sometimes disappear if they have no point in a tile")
                        .SetDefault("14")
                };

        private const bool IsStable = true;


        public WriteVectorTiles
            () :
            base(_names, About, _extraParams, IsStable)
        {
        }

        private Dictionary<string, List<string>> CalculateRoutes(FeatureCollection addFeatures, TransitDbSnapShot tdb)
        {
            var connections = tdb.ConnectionsDb;

            var routes = new RouteMerger(connections);
            var routeId = (uint) 0;

            var stops2Routes = new Dictionary<string, List<string>>();
            foreach (var (route, trips) in routes.GetRouteToTrips())
            {
                var feature = new RouteFeature(tdb, route, routeId,
                    tdb.TripsDb.GetAll(trips), tdb.GlobalId);
                addFeatures.Add(feature);


                foreach (var stopId in route)
                {
                    var stop = tdb.StopsDb.Get(stopId);
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

        public void Use(Dictionary<string, string> arguments, IEnumerable<TransitDbSnapShot> tdbs)
        {
            var writeTo = arguments["directory"];

            var minZoom = uint.Parse(arguments["minzoom"]);
            var maxZoom = uint.Parse(arguments["maxzoom"]);

            Console.WriteLine(
                $"Generating vector tiles to directory {writeTo} for zoom levels {minZoom} --> {maxZoom}");

            if (File.Exists(writeTo))
            {
                throw new ArgumentException("The target directory " + writeTo + " already exists, but it is a file");
            }

            if (!Directory.Exists(writeTo))
            {
                Directory.CreateDirectory(writeTo);
            }


            var features = new FeatureCollection();
            var bbox = new BBox();
            var sources = "";

            var oldFeatureCount = 0;
            try
            {

                foreach (var tdb in tdbs)
                {
                    var stops2Routes = CalculateRoutes(features, tdb);
                    var bboxTdb = AddStops(tdb, features, stops2Routes);
                    bbox.AddBBox(bboxTdb);
                    var source = tdb.GetAttribute("name", tdb.GlobalId);
                    sources += source + ";";
                    var featureCount = features.Count;

                    Console.WriteLine(source + " generated " + (featureCount - oldFeatureCount) + " features");

                    oldFeatureCount = featureCount;
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

                Console.WriteLine("Writing tiles...");
                new VectorTileTree {{features, ConfigureFeature}}.Write(writeTo);


                var mvtFileContents = GenerateMvtJson(
                    "Public transport data", "Information about a public transport operator",
                    $"Data from {sources} - generated with Itinero.Transit",
                    "https://anyways.eu", "",
                    bbox.ToJson(), minZoom, maxZoom,
                    new[]
                    {
                        ("stops", "The location of all the stops"),
                        ("routes", "All the routes (which can have multiple services a day)")
                    });

                File.WriteAllText(Path.Combine(writeTo, "mvt.json"), mvtFileContents);
            }
            catch (Exception e)
            {
                throw new Exception("Something went wrong creating vector tiles", e); // Repackage as real exception as to trigger a stack trace
            }

            Console.WriteLine("Vector tile exportation complete");
        }

        private static BBox AddStops(TransitDbSnapShot tdb,
            FeatureCollection features, Dictionary<string, List<string>> stops2Routes)
        {
            var stops = tdb.StopsDb;

            var empty = new List<string>();
            var bbox = new BBox();
            foreach (var stop in stops)
            {
                features.Add(
                    new StopFeature(stop, tdb.GlobalId,
                        stops2Routes.GetValueOrDefault(stop.GlobalId, empty)));

                bbox.AddCoordinate((stop.Longitude, stop.Latitude));
            }

            return bbox;
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
            string bounds, uint minZoom, uint maxZoom,
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

    internal class BBox
    {
        private double _minLat = double.MaxValue;
        private double _minLon = double.MaxValue;
        private double _maxLat = double.MinValue;
        private double _maxLon = double.MinValue;


        public void AddCoordinate((double Longitude, double Latitude) c)
        {
            _minLat = Math.Min(c.Latitude, _minLat);
            _minLon = Math.Min(c.Longitude, _minLon);
            _maxLat = Math.Max(c.Latitude, _maxLat);
            _maxLon = Math.Max(c.Longitude, _maxLon);
        }

        public void AddBBox(BBox bbox)
        {
            AddCoordinate((bbox._minLon, bbox._minLat));
            AddCoordinate((bbox._maxLon, bbox._maxLat));
        }

        public string ToJson()
        {
            return $"[{_minLon}, {_minLat}, {_maxLon}, {_maxLat}]";
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