using System;
using System.Collections.Generic;

namespace Itinero.Transit
{
    public class LocationCombiner : ILocationProvider
    {
        private readonly BoundingBox _bbox;
        private readonly IEnumerable<ILocationProvider> _providers;

        public LocationCombiner(params ILocationProvider[] sources)
            : this(new List<ILocationProvider>(sources))
        {
            
        }
        
        public LocationCombiner(IReadOnlyList<ILocationProvider> backdrops)
        {
            _providers = backdrops;
            _bbox = backdrops[0].BBox();
            foreach (var prov in backdrops)
            {
                _bbox = _bbox.Expand(prov.BBox());
            }
        }

        public bool ContainsLocation(Uri locationId)
        {
            foreach (var provider in _providers)
            {
                if (provider.ContainsLocation(locationId))
                {
                    return true;
                }
            }

            return false;
        }

        public Location GetCoordinateFor(Uri locationId)
        {
            foreach (var provider in _providers)
            {
                if (provider.ContainsLocation(locationId))
                {
                    return provider.GetCoordinateFor(locationId);
                }
            }

            throw new KeyNotFoundException($"This combiner does not contain {locationId}");
        }

        public IEnumerable<Uri> GetLocationsCloseTo(float lat, float lon, int radiusInMeters)
        {
            var allLocations = new HashSet<Uri>();
            foreach (var provider in _providers)
            {
                allLocations.UnionWith(provider.GetLocationsCloseTo(lat, lon, radiusInMeters));
            }

            return allLocations;
        }

        public IEnumerable<Location> GetLocationByName(string name)
        {
            var allLocations = new HashSet<Location>();
            foreach (var provider in _providers)
            {
                allLocations.UnionWith(provider.GetLocationByName(name));
            }

            return allLocations;
        }

        public BoundingBox BBox()
        {
            return _bbox;
        }
    }
}