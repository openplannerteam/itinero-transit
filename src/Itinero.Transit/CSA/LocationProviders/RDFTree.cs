using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Itinero.LocalGeo;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

namespace Itinero.Transit
{
    /// <summary>
    /// A node in the RDF-tree, which pointers to it's children.
    /// Keeps track of a few bounding boxes and to what subtree they refer
    /// Note that it might not have been downloaded yet!
    /// </summary>
    [Serializable]
    public class RdfTree : LinkedObject
    {
        // This object is shared between all nodes of the same tree; they all add their fragments to it
        private readonly Dictionary<string, RdfTree> _allTrees;
        private readonly List<BoundingBox> _bounds = new List<BoundingBox>();

        private readonly List<Uri> _subtrees = new List<Uri>();

        // The 'hashcode' of Uri is very broken
        public HashSet<string> Members { get; } = new HashSet<string>();

        private BoundingBox _bbox;

        public RdfTree(Dictionary<string, RdfTree> allTrees, Uri uri) : base(uri)
        {
            _allTrees = allTrees;
        }

        public RdfTree(Dictionary<string, RdfTree> allTrees, JObject json) : base(json.GetId())
        {
            _allTrees = allTrees;
            FromJson(json);
        }

        public HashSet<RdfTree> GetOverlappingTrees(BoundingBox box, JsonLdProcessor proc)
        {
            var found = new HashSet<RdfTree>();
            GetOverlappingTrees(box, proc, found);
            return found;
        }

        private void GetOverlappingTrees(BoundingBox box, JsonLdProcessor proc, HashSet<RdfTree> found)
        {
            if (Members.Count != 0 && _bbox.Overlaps(box))
            {
                found.Add(this);
            }

            for (int i = 0; i < _bounds.Count; i++)
            {
                if (!_bounds[i].Overlaps(box))
                {
                    continue;
                }

                var treeId = _subtrees[i];
                RdfTree subtree;
                if (!_allTrees.ContainsKey(treeId.ToString()))
                {
                    // We search a node, which is in a fragment as denoted by the treeId URI
                    // E.g. tree1.json#1234 -> we download the entire tree, normally 1234 will be downloaded and added to the dict
                    new RdfTree(_allTrees, treeId).Download(proc);
                }

                subtree = _allTrees[treeId.ToString()];

                subtree.GetOverlappingTrees(box, proc, found);
            }
        }

        protected sealed override void FromJson(JObject json)
        {
            if (json.IsDictContaining("@graph", out var d))
            {
                json = (JObject) d["@graph"][0];
            }

            json.AssertTypeIs("https://w3id.org/tree#Node");
            Uri = json.GetId();

            /* There are three cases:
                1) The Node is a leaf containing members
                2) THe node contains other nodes
                3) The node is only a link to another fragment
                
                Only in the first two cases, the RDFTree should be added to the _allTrees dict
            */

            var membersID = "http://www.w3.org/ns/hydra/core#member";
            var hasChilds = "https://w3id.org/tree#hasChildRelation";

            if (json.IsDictContaining(membersID, out _) || json.IsDictContaining(hasChilds, out _))
            {
                _allTrees.Add(Uri.ToString(), this);
            }


            _bbox = new BoundingBox(json);

            if (json.IsDictContaining(membersID, out var dct))
            {
                // Add members, if any are present
                var memberList = dct["http://www.w3.org/ns/hydra/core#member"];
                foreach (var member in (JArray) memberList)
                {
                    Members.Add(member.GetId().ToString());
                }
            }

            // Add children, if any are present
            // ReSharper disable once InvertIf
            if (json.IsDictContaining(hasChilds, out _))
            {
                var childRelation = (JObject) json[hasChilds][0];

                childRelation.AssertTypeIs("https://w3id.org/tree#GeospatiallyContainsRelation");

                var childs = (JArray) childRelation["https://w3id.org/tree#child"];
                foreach (var jToken in childs)
                {
                    var child = (JObject) jToken;
                    child.AssertTypeIs("https://w3id.org/tree#Node");

                    // We add the bounds and the ID to find our way around
                    _subtrees.Add(child.GetId());
                    _bounds.Add(new BoundingBox(child));

                    // Does the JSON contain all the necessary information? Or are we working with a pointer?
                    // IF we work with but a pointer, we continue
                    // IF not, we instantiate

                    // The tree will register itself if needed in the 'allTrees' dictionary
                    // Hence no need to save the tree somewhere
                    // ReSharper disable once ObjectCreationAsStatement
                    new RdfTree(_allTrees, child);
                }
            }
        }

        public override string ToString()
        {
            var kids = "";
            for (var i = 0; i < Math.Min(10, _bounds.Count); i++)
            {
                kids += $"  {_bounds[i]} --> {_subtrees[i]}\n";
            }

            if (_bounds.Count > 10)
            {
                kids += "...\n";
            }

            return $"RDFTreeNode {Uri} with {_bounds.Count} elements within {_bbox}:\n{kids}";
        }


        public BoundingBox BBox()
        {
            return _bbox;
        }
    }


    [Serializable]
    public class BoundingBox
    {
        private readonly Polygon _outline;
        private static readonly float LatDiffFactor = 1f/ (60 * 1852);


        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public BoundingBox(float minLat, float maxLat, float minLon, float maxLon)
        {
            var _minLat = Math.Min(minLat, maxLat);
            var _maxLat = Math.Max(minLat, maxLat);
            var _minLon = Math.Min(minLon, maxLon);
            var _maxLon = Math.Max(minLon, maxLon);
            _outline = new Polygon()
            {
                ExteriorRing = new List<Coordinate>()
                {
                    new Coordinate(_minLat, _minLon),
                    new Coordinate(_minLat, _maxLon),
                    new Coordinate(_maxLat, _maxLon),
                    new Coordinate(_maxLat, _minLon),
                    new Coordinate(_minLat, _minLon)
                }
            };
        }

        public BoundingBox(float lat, float lon, int radiusInMeters) :
            this(lat - (radiusInMeters * LatDiffFactor), lat + (radiusInMeters * LatDiffFactor),
                lon - LonDiff(radiusInMeters, lat), lon + LonDiff(radiusInMeters, lat))
        {
        }

        private static float LonDiff(int radiusInMeters, float lat)
        {
            var latDiff = radiusInMeters * LatDiffFactor;
            var lonDiff = (float) (latDiff * Math.Cos(lat));
            return lonDiff;
        }

        public BoundingBox(JObject json)
        {
            if (json.IsDictContaining("https://w3id.org/tree#value", out var d))
            {
                json = (JObject) d["https://w3id.org/tree#value"][0];
            }

            json.AssertTypeIs("http://www.opengis.net/ont/geosparql#wktLiteral");
            var val = json.GetLDValue();
            val = val.Substring("POLYGON ((".Length);
            val = val.Substring(0, val.Length - 2);
            var parts = val.Split(',');

            _outline = new Polygon();

            foreach (var coor in parts)
            {
                var coordinate = new Coordinate(ExtractValue(coor, 1), ExtractValue(coor, 0));
                _outline.ExteriorRing.Add(coordinate);
            }
        }

        private static float ExtractValue(string coordinate, int index)
        {
            return float.Parse(coordinate.Split()[index]);
        }

        public bool IsContained(float lat, float lon)
        {
            return _outline.PointIn(new Coordinate(lat, lon));
        }

        public bool Overlaps(BoundingBox other)
        {
            foreach (var coor in other._outline.ExteriorRing)
            {
                if (_outline.PointIn(coor))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsContained(BoundingBox other)
        {
            foreach (var coor in other._outline.ExteriorRing)
            {
                if (!_outline.PointIn(coor))
                {
                    return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            return $"BBox {_outline}";
        }

        /// <summary>
        /// Creates a new bounding box that bounds both this and the other box
        /// </summary>
        /// <param name="bBox"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public BoundingBox Expand(BoundingBox bBox)
        {
            var minLat = 180f;
            var minLon = 180f;
            var maxLat = -180f;
            var maxLon = -180f;

            foreach (var l in _outline.ExteriorRing)
            {
                minLat = Math.Min(l.Latitude, minLat);
                minLon = Math.Min(l.Longitude, minLon);
                maxLat = Math.Max(l.Latitude, maxLat);
                maxLon = Math.Max(l.Longitude, maxLon);
            }

            foreach (var l in bBox._outline.ExteriorRing)
            {
                minLat = Math.Min(l.Latitude, minLat);
                minLon = Math.Min(l.Longitude, minLon);
                maxLat = Math.Max(l.Latitude, maxLat);
                maxLon = Math.Max(l.Longitude, maxLon);
            }

            return new BoundingBox(minLat, maxLat, minLon, maxLon);
        }
    }
}