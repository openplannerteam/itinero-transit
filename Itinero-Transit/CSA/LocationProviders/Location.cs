using System;
using Itinero_Transit.LinkedData;
using JsonLD.Core;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Itinero_Transit.CSA.ConnectionProviders.LinkedConnection
{
    /// <summary>
    /// Represents a transit location, such as a bus stop or train station
    /// </summary>
    [Serializable]
    public class Location : LinkedObject
    {
        public string Name;
        public float Lat, Lon;

        public Location(Uri uri) : base(uri)
        {
        }

        public Location(JObject obj) : base(obj.GetId())
        {
            FromJson(obj);
        }

        protected sealed override void FromJson(JObject json)
        {
            Lat = json.GetFloat("http://www.w3.org/2003/01/geo/wgs84_pos#lat");
            Lon = json.GetFloat("http://www.w3.org/2003/01/geo/wgs84_pos#long");
            Name = json.GetLDValue("http://xmlns.com/foaf/0.1/name");
        }

        public override string ToString()
        {
            return $"Location '{Name}' ({Uri}) at coordinates {Lat},{Lon} ";
        }
    }
}