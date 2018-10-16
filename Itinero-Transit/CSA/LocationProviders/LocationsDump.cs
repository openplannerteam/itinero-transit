using System;
using Itinero_Transit.LinkedData;
using Newtonsoft.Json.Linq;
using Reminiscence.Collections;
using Serilog;

namespace Itinero_Transit.CSA.ConnectionProviders.LinkedConnection
{
    /// <summary>
    /// This class is (on of) the actual classes that searches station locations.
    /// It's a very naive implementation - a brute force (but simple) approach.
    /// This class is meant to handle providers which offer their station data as a single big dump (such as the SNCB)
    /// </summary>
    public class LocationsDump : LinkedObject
    {

        private readonly List<Location> _locations = new List<Location>();
        
        public LocationsDump(Uri uri) : base(uri)
        {
        }

        protected override void FromJson(JObject json)
        {
            foreach (var loc in json["@graph"])
            {
                _locations.Add(new Location((JObject) loc));
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
    }
}