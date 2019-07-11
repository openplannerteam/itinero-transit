using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Itinero.Transit.IO.LC.Data
{
    /// <summary>
    /// This class is (on of) the actual classes that searches station locations.
    /// It's a very naive implementation - a brute force (but simple) approach.
    /// This class is meant to handle providers which offer their station data as a single big dump (such as the SNCB)
    /// </summary>
    [Serializable]
    public class LocationProvider : LinkedObject
    {
        public Uri Uri { get; }
        public readonly List<Location> Locations = new List<Location>();

        private readonly Dictionary<string, Location> _locationMapping = new Dictionary<string, Location>();

        private readonly Dictionary<string, HashSet<Location>> _nameMapping =
            new Dictionary<string, HashSet<Location>>();

        private float _minLat, _maxLat, _minLon, _maxLon;

        public LocationProvider(Uri uri)
        {
            Uri = uri;
        }

        public void FromJson(JObject json)
        {
            _minLat = 180f;
            _minLon = 180f;
            _maxLat = -180f;
            _maxLon = -180f;
            foreach (var loc in json["@graph"])
            {
                var l = new Location((JObject) loc);
                Locations.Add(l);
            }

            ProcessLocations();
        }

        protected void ProcessLocations()
        {
            foreach (var l in Locations)
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
        
        public string GetNameOf(Uri uri)
        {
            return $"{GetCoordinateFor(uri).Name} ({uri.Segments.Last()})";
        }

        public override string ToString()
        {
//            var overview = "";
//            foreach (var location in Locations)
//            {
//                overview += "  " + location + "\n";
//            }
//
//            return $"Location dump with {Locations.Count} locations:\n{overview}";
            return string.Empty;
        }

        public Location GetCoordinateFor(Uri locationId)
        {
            if (_locationMapping.TryGetValue(locationId.ToString(), out var value))
            {
                return value;
            }

            return null;
        }

        // ReSharper disable once UnusedMember.Global
        public IEnumerable<Location> GetLocationByName(string name)
        {
            return _nameMapping[name];
        }
    }
}