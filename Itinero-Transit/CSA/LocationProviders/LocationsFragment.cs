using System;
using System.Collections.Generic;
using System.Linq;
using Itinero_Transit.CSA.LocationProviders;
using Itinero_Transit.LinkedData;
using Newtonsoft.Json.Linq;

namespace Itinero_Transit.CSA.ConnectionProviders.LinkedConnection
{
    /// <summary>
    /// This class is (on of) the actual classes that searches station locations.
    /// It's a very naive implementation - a brute force (but simple) approach.
    /// This class is meant to handle providers which offer their station data as a single big dump (such as the SNCB)
    /// </summary>
    [Serializable]
    public class LocationsFragment : LinkedObject, ILocationProvider
    {
        private readonly Reminiscence.Collections.List<Location> _locations =
            new Reminiscence.Collections.List<Location>();

        private readonly Dictionary<string, Location> _locationMapping = new Dictionary<string, Location>();

        private readonly Dictionary<string, HashSet<Location>> _nameMapping =
            new Dictionary<string, HashSet<Location>>();

        private BoundingBox _bounds;

        public LocationsFragment(Uri uri) : base(uri)
        {
        }

        protected override void FromJson(JObject json)
        {
            var minLat = 180f;
            var minLon = 180f;
            var maxLat = -180f;
            var maxLon = -180f;
            foreach (var loc in json["@graph"])
            {
                var l = new Location((JObject) loc);
                _locations.Add(l);
                _locationMapping.Add(l.Uri.ToString(), l);

                if (!_nameMapping.ContainsKey(l.Name))
                {
                    _nameMapping.Add(l.Name, new HashSet<Location>());
                }

                _nameMapping[l.Name].Add(l);

                minLat = Math.Min(l.Lat, minLat);
                minLon = Math.Min(l.Lon, minLon);
                maxLat = Math.Max(l.Lat, maxLat);
                maxLon = Math.Max(l.Lon, maxLon);
            }

            _bounds = new BoundingBox(minLat, maxLat, minLon, maxLon);
        }

        public override string ToString()
        {
            var overview = "";
            foreach (var location in _locations)
            {
                overview += "  " + location + "\n";
            }

            return $"Location dump with {_locations.Count} locations:\n{overview}";
        }

        public bool ContainsLocation(Uri locationId)
        {
            return _locationMapping.ContainsKey(locationId.ToString());
        }

        public Location GetCoordinateFor(Uri locationId)
        {
            if (!_locationMapping.ContainsKey(locationId.ToString()))
            {
                var examples = "";
                var keys = new List<string>(_locationMapping.Keys).GetRange(0, 10);
                foreach (var key in keys)
                {
                    examples += $"  {key}\n";
                }

                throw new KeyNotFoundException(
                    $"The location {locationId} was not found in this dictionary.\nSome keys in this dictionary are:\n{examples}");
            }

            return _locationMapping[locationId.ToString()];
        }

        public HashSet<Location> GetLocationByName(string name)
        {
            return _nameMapping[name];
        }

        public IEnumerable<Uri> GetLocationsCloseTo(float lat, float lon, int radiusInMeters)
        {
            if (radiusInMeters < 1)
            {
                throw new ArgumentNullException("The radius in which locations are sought, should be at least 1m");
            }

            if (!_bounds.Overlaps(new BoundingBox(lat, lon, radiusInMeters)))
            {
                return Enumerable.Empty<Uri>();
            }

            var closeEnough = new List<Uri>();

            foreach (var l in _locations)
            {
                var d = DistanceBetweenPoints.DistanceInMeters(lat, lon, l.Lat, l.Lon);

                if (d < radiusInMeters)
                {
                    closeEnough.Add(l.Uri);
                }
            }

            return closeEnough;
        }

        public BoundingBox BBox()
        {
            return _bounds;
        }
    }
}