//using System;
//using System.Collections.Generic;
//using JsonLD.Core;
//
//namespace Itinero.Transit.IO.LC.CSA.LocationProviders
//{
//    /// <inheritdoc />
//    ///  <summary>
//    ///  This class uses RDF-trees to search for locations.
//    ///  How does this thing work?
//    ///  TODO move upstream, perhaps into JSON-LD
//    ///  </summary>
//    internal class RdfTreeTraverser : ILocationProvider
//    {
//        private readonly JsonLdProcessor _treeNodeLoader, _locationFragmentLoader;
//
//        private readonly RdfTree _root;
//        private readonly Dictionary<string, RdfTree> _knownParts = new Dictionary<string, RdfTree>();
//
//        /// 'Caches' the location fragment files that have already been downloaded
//        /// Used to quickly get a fragment (and its contained location) by ID
//        private readonly Dictionary<string, LocationsFragment> _fragments = new Dictionary<string, LocationsFragment>();
//
//
//        public RdfTreeTraverser(Uri treeRoot,
//            JsonLdProcessor treeNodeLoader, JsonLdProcessor locationFragmentLoader)
//        {
//            _treeNodeLoader = treeNodeLoader;
//            _locationFragmentLoader = locationFragmentLoader;
//
//            _root = new RdfTree(_knownParts, treeRoot);
//            _root.Download(_treeNodeLoader);
//        }
//
//        public Location GetCoordinateFor(Uri locationId)
//        {
//            var fragName = CacheFragment(locationId);
//            return _fragments[fragName].GetCoordinateFor(locationId);
//        }
//
//        public bool ContainsLocation(Uri locationId)
//        {
//            var fragName = CacheFragment(locationId);
//            return _fragments.ContainsKey(fragName) && _fragments[fragName].ContainsLocation(locationId);
//        }
//
//        /// <summary>
//        /// Gives the fragment name; chaching the fragment if needed
//        /// </summary>
//        /// <param name="locationId"></param>
//        /// <returns></returns>
//        private string CacheFragment(Uri locationId)
//        {
//            var fragmentsName = locationId.GetLeftPart(UriPartial.Path);
//            if (!_fragments.ContainsKey(fragmentsName))
//            {
//                var frag = new LocationsFragment(locationId);
//                frag.Download(_locationFragmentLoader);
//                _fragments.Add(fragmentsName, frag);
//            }
//
//            return fragmentsName;
//        }
//
////        public IEnumerable<Uri> GetLocationsCloseTo(float lat, float lon, int radiusInMeters)
////        {
////            // First, we start by figuring out which RDFNodes we exactly need
////            var bbox = new BoundingBox(lat, lon, radiusInMeters);
////            var nodesToConsider = _root.GetOverlappingTrees(bbox, _treeNodeLoader);
////
////            // And now we have a look to all the members of those nodes; and only keep the ones withing the circle
////            var found = new List<Uri>();
////
////            foreach (var node in nodesToConsider)
////            {
////                foreach (var member in node.Members)
////                {
////                    var location = GetCoordinateFor(new Uri(member));
////
////                    if (Coordinate.DistanceEstimateInMeter(location.Lat, location.Lon, lat, lon) <= radiusInMeters)
////                    {
////                        found.Add(location.Uri);
////                    }
////                }
////            }
////
////            return found;
////        }
////
////        public BoundingBox BBox()
////        {
////            return _root.BBox();
////        }
//
//        public IEnumerable<Location> GetLocationByName(string name)
//        {
//            throw new NotImplementedException();
//        }
//
//        public IEnumerable<Location> GetAllLocations()
//        {
//            throw new NotImplementedException();
//        }
//    }
//}