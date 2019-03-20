using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]
namespace Itinero.Transit.IO.LC.Data
{
    /// <summary>
    /// Represents a transit location, such as a bus stop or train station
    /// </summary>
    [Serializable]
    public class Location : LinkedObject
    {
        public string Name;
        public float Lat, Lon;

        public (string lang, string name)[] Names { get; set; }

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
            if (json.TryGetValue("http://purl.org/dc/terms/alternative", out var alternativeNamesToken))
            {
                var names = new List<(string lang, string name)>();
                foreach (var val in alternativeNamesToken)
                {
                    var lang = val.GetContents("@language") is JValue langVal ? langVal.ToString(CultureInfo.InvariantCulture) : string.Empty;
                    var name = val.GetContents("@value") is JValue nameVal ? nameVal.ToString(CultureInfo.InvariantCulture) : string.Empty;
                    if (!string.IsNullOrWhiteSpace(lang))
                    {
                        names.Add((lang, name));
                    }
                }

                Names = names.ToArray();
            }
        }

        public override string ToString()
        {
            return $"Location '{Name}' ({Uri}) at coordinates {Lat},{Lon} ";
        }
    }
}