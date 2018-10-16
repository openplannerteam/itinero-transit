using System;
using System.Collections.Generic;
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
    public class LocationsDump : LinkedObject, ILocationProvider
    {

        private readonly Reminiscence.Collections.List<Location> _locations = new Reminiscence.Collections.List<Location>();
        private readonly Dictionary<Uri, Location> _locationMapping = new Dictionary<Uri, Location>();
        private readonly Dictionary<string, Location> _nameMapping = new Dictionary<string, Location>();
        
        public LocationsDump(Uri uri) : base(uri)
        {
        }

        protected override void FromJson(JObject json)
        {
            foreach (var loc in json["@graph"])
            {
                var l = new Location((JObject) loc);
                _locations.Add(l);
                _locationMapping.Add(l.Uri, l);
                _nameMapping.Add(l.Name, l);
            }
        }

        public override string ToString()
        {
            var overview = "";
            foreach (var location in _locations)
            {
                overview += "  "+location + "\n";
            }

            return $"Location dump with {_locations.Count} locations:\n{overview}";
        }

        public Location GetCoordinateFor(Uri locationId)
        {
            return _locationMapping[locationId];
        }

        public Uri GetLocationByName(string name)
        {
            return _nameMapping[name].Uri;
        }

        public IEnumerable<Uri> GetLocationsCloseTo(float lat, float lon, int radiusInMeters)
        {

            if (radiusInMeters < 1)
            {
                throw new ArgumentNullException("The radius in which locations are sought, should be at least 1m");
            }
            
            var closeEnough = new HashSet<Uri>();
            
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
    }
}