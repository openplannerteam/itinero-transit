using System;
using System.Collections.Generic;
using Itinero_Transit.CSA.LocationProviders;
using Itinero_Transit.LinkedData;
using JsonLD.Core;

namespace Itinero_Transit.CSA.ConnectionProviders.LinkedConnection.TreeTraverse
{
    /// <inheritdoc />
    ///  <summary>
    ///  This class uses RDF-trees to search for locations.
    ///  How does this thing work?
    ///  TODO move upstream, perhaps into JSON-LD
    ///  </summary>
    public class RdfTreeTraverser : ILocationProvider
    {
        private readonly JsonLdProcessor _coordinateLoader;
        
        private readonly RdfTree _root;
        private readonly Dictionary<string, RdfTree> _knownParts = new Dictionary<string, RdfTree>();
        
        /// 'Caches' the location fragment files that have already been downloaded
        /// Used to quickly get a fragment (and its contained location) by ID
        private readonly Dictionary<string, LocationsFragment> _fragments = new Dictionary<string, LocationsFragment>();

        
        
        
        public RdfTreeTraverser(Uri treeRoot, JsonLdProcessor proc)
        {
            _coordinateLoader = proc;

            _root = new RdfTree(_knownParts, treeRoot);
            _root.Download(_coordinateLoader);
        }


        public Location GetCoordinateFor(Uri locationId)
        {
            var fragmentsName = locationId.GetLeftPart(UriPartial.Path);
            if (!_fragments.ContainsKey(fragmentsName))
            {
                var frag = new LocationsFragment(locationId);
                frag.Download(_coordinateLoader);
                _fragments.Add(fragmentsName, frag);
            }

            return _fragments[fragmentsName].GetCoordinateFor(locationId);
        }


        public IEnumerable<Uri> GetLocationsCloseTo(float lat, float lon, int radiusInMeters)
        {
            
            // First, we start by figuring out which RDFNodes we exactly need
            var bbox=  new BoundingBox(lat - radiusInMeters, lat + radiusInMeters, lon - radiusInMeters, lon + radiusInMeters);
            var nodesToConsider = _root.GetOverlappingTrees(bbox, _coordinateLoader);
            
            
            
            throw new NotImplementedException();
        }
    }

    
    
}