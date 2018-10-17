using System;
using System.Collections.Generic;
using System.Linq;
using Itinero_Transit.LinkedData;
using JsonLD.Core;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Itinero_Transit.CSA.LocationProviders
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
                if (_bounds[i].Overlaps(box))
                {
                    var treeId = _subtrees[i];
                    RdfTree subtree;
                    if (!_allTrees.ContainsKey(treeId.ToString()))
                    {
                        subtree = new RdfTree(_allTrees, treeId);
                        subtree.Download(proc);
                    }
                    else
                    {
                        subtree = _allTrees[treeId.ToString()];
                    }

                    subtree.GetOverlappingTrees(box, proc, found);
                }
            }
        }

        protected sealed override void FromJson(JObject json)
        {
            if (json.IsDictContaining("@graph", out var d))
            {
                json = (JObject) d["@graph"][0];
            }

            Uri = json.GetId();
            if (_allTrees.ContainsKey(Uri.ToString()))
            {
                Log.Warning($"Already have {Uri}, why download again?");
                return;
            }

            _allTrees.Add(Uri.ToString(), this);


            json.AssertTypeIs("https://w3id.org/tree#Node");
            _bbox = new BoundingBox(json);

            var childRelation = (JObject) json["https://w3id.org/tree#hasChildRelation"][0];

            childRelation.AssertTypeIs("https://w3id.org/tree#GeospatiallyContainsRelation");

            var childs = (JArray) childRelation["https://w3id.org/tree#child"];
            foreach (JObject child in childs)
            {
                child.AssertTypeIs("https://w3id.org/tree#Node");

                _subtrees.Add(child.GetId());
                _bounds.Add(new BoundingBox(child));

                if (child.IsDictContaining("https://w3id.org/tree#hasChildRelation", out var subtree))
                {
                    // The tree will register itself in the 'allTrees' dictionary
                    // Hence no need to save the tree somewhere
                    // ReSharper disable once ObjectCreationAsStatement
                    new RdfTree(_allTrees, child);
                }

                // ReSharper disable once InvertIf
                if (child.IsDictContaining("http://www.w3.org/ns/hydra/core#member", out var dct))
                {
                    var memberList = dct["http://www.w3.org/ns/hydra/core#member"];
                    foreach (var member in (JArray) memberList )
                    {
                        Members.Add(member.GetId().ToString());
                    }
                }
            }
        }

        public override string ToString()
        {
            var kids = "";
            for (int i = 0; i < Math.Min(10, _bounds.Count); i++)
            {
                kids += $"  {_bounds[i]} --> {_subtrees[i]}\n";
            }

            if (_bounds.Count > 10)
            {
                kids += "...\n";
            }

            return $"RDFTreeNode {Uri} with {_bounds.Count} elements within {_bbox}:\n{kids}";
        }
    }


    [Serializable]
    public class BoundingBox
    {
        private readonly float _minLat, _maxLat, _minLon, _maxLon;

        public BoundingBox(float minLat, float maxLat, float minLon, float maxLon)
        {
            _minLat = Math.Min(minLat, maxLat);
            _maxLat = Math.Max(minLat, maxLat);
            _minLon = Math.Min(minLon, maxLon);
            _maxLon = Math.Max(minLon, maxLon);
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
            var parts = val.Split(", ");


            var minLon = extractValue(parts[0], 0);
            var minLat = extractValue(parts[0], 1);
            var maxLon = extractValue(parts[2], 0);
            var maxLat = extractValue(parts[2], 1);
            _minLat = Math.Min(minLat, maxLat);
            _maxLat = Math.Max(minLat, maxLat);
            _minLon = Math.Min(minLon, maxLon);
            _maxLon = Math.Max(minLon, maxLon);
        }

        private static float extractValue(string coordinate, int index)
        {
            return float.Parse(coordinate.Split()[index]);
        }

        public bool IsContained(float lat, float lon)
        {
            return _minLat <= lat
                   && _maxLat >= lat
                   && _minLon <= lon
                   && _maxLon >= lon;
        }

        public bool Overlaps(BoundingBox other)
        {
            return IsContained(other._minLat, other._minLon)
                   || IsContained(other._minLat, other._maxLon)
                   || IsContained(other._maxLat, other._minLon)
                   || IsContained(other._maxLat, other._maxLon);
        }

        public bool IsContained(BoundingBox other)
        {
            return IsContained(other._minLat, other._minLon)
                   && IsContained(other._minLat, other._maxLon)
                   && IsContained(other._maxLat, other._minLon)
                   && IsContained(other._maxLat, other._maxLon);
        }

        public override string ToString()
        {
            return $"BBox {_minLat}, {_minLon}; {_maxLat}, {_maxLon}";
        }
    }
}