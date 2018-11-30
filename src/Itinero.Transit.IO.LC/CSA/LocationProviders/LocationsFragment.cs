using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.LocalGeo;
using Newtonsoft.Json.Linq;

namespace Itinero.Transit
{
    /// <inheritdoc cref="ILocationProvider" />
    /// <summary>
    /// This class is (on of) the actual classes that searches station locations.
    /// It's a very naive implementation - a brute force (but simple) approach.
    /// This class is meant to handle providers which offer their station data as a single big dump (such as the SNCB)
    /// </summary>
    [Serializable]
    public class LocationsFragment : LinkedObject, ILocationProvider
    {
        private readonly List<Location> _locations = new List<Location>();

        private readonly Dictionary<string, Location> _locationMapping = new Dictionary<string, Location>();

        private readonly Dictionary<string, HashSet<Location>> _nameMapping =
            new Dictionary<string, HashSet<Location>>();

        private float _minLat, _maxLat, _minLon, _maxLon;

        private static readonly IEnumerable<Uri> Empty = new List<Uri>();

        [NonSerialized] private BoundingBox _bounds;

        public LocationsFragment(Uri uri) : base(uri)
        {
        }

        /// <summary>
        /// Constructor with a number of preloaded locations.
        /// Mainly used for testing
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="locations"></param>
        public LocationsFragment(Uri uri, IEnumerable<Location> locations)
            : base(uri)
        {
            _locations = new List<Location>(locations);
            ProcessLocations();
        }

        protected override void FromJson(JObject json)
        {
            _minLat = 180f;
            _minLon = 180f;
            _maxLat = -180f;
            _maxLon = -180f;
            foreach (var loc in json["@graph"])
            {
                var l = new Location((JObject) loc);
                _locations.Add(l);
            }

            ProcessLocations();
        }

        protected void ProcessLocations()
        {
            foreach (var l in _locations)
            {
                _locationMapping.Add(l.Uri.ToString(), l);

                if (!_nameMapping.ContainsKey(l.Name))
                {
                    _nameMapping.Add(l.Name, new HashSet<Location>());
                }

                _nameMapping[l.Name].Add(l);

                _minLat = Math.Min(l.Lat, _minLat);
                _minLon = Math.Min(l.Lon, _minLon);
                _maxLat = Math.Max(l.Lat, _maxLat);
                _maxLon = Math.Max(l.Lon, _maxLon);
            }
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

        // ReSharper disable once UnusedMember.Global
        public IEnumerable<Location> GetLocationByName(string name)
        {
            return _nameMapping[name];
        }

        public IEnumerable<Uri> GetLocationsCloseTo(float lat, float lon, int radiusInMeters)
        {
            if (radiusInMeters < 1)
            {
                return Empty;
            }

            if (!BBox().Overlaps(new BoundingBox(lat, lon, radiusInMeters)))
            {
                return Enumerable.Empty<Uri>();
            }

            var closeEnough = new List<Uri>();

            foreach (var l in _locations)
            {
                var d = Coordinate.DistanceEstimateInMeter(lat, lon, l.Lat, l.Lon);
                if (d < radiusInMeters)
                {
                    closeEnough.Add(l.Uri);
                }
            }

            return closeEnough;
        }

        public BoundingBox BBox()
        {
            return _bounds ?? (_bounds = new BoundingBox(_minLat, _maxLat, _minLon, _maxLon));
        }
    }
}