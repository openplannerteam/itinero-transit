using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Algorithms.Mergers;
using Itinero.Transit.Data;
using Itinero.Transit.IO.VectorTiles;
using NetTopologySuite.Features;
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
                    SwitchesExtensions.opt("maxzoom",
                            "The maximal zoom level that the vector tiles are generated for. Note: maxzoom should be pretty big, as lines sometimes disappear if they have no point in a tile")
                        .SetDefault("14"),
                    SwitchesExtensions.opt("extent", "resolution",
                            "The precision of every vector tile. Increase this value if the locations are not good enough on high zoom levels")
                        .SetDefault("4096")
                };

        private const bool IsStable = true;


        public WriteVectorTiles
            () :
            base(_names, About, _extraParams, IsStable)
        {
        }

        public void Use(Dictionary<string, string> arguments, IEnumerable<TransitDbSnapShot> tdbs)
        {
            var writeTo = arguments["directory"];

            var minZoom = uint.Parse(arguments["minzoom"]);
            var maxZoom = uint.Parse(arguments["maxzoom"]);
            var extent = uint.Parse(arguments["extent"]);

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


            try
            {
                var (tree, bbox, sources) = tdbs.CalculateVectorTileTree(minZoom, maxZoom);
                tree.Write(writeTo, extent);
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
                throw new Exception("Something went wrong creating vector tiles",
                    e); // Repackage as real exception as to trigger a stack trace
            }

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
}