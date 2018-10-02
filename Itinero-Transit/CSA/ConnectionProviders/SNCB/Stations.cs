using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Itinero_Transit.LinkedData
{
    /**
     * Dumb class providing a mapping from the URI of a station to a human readable name.
     * Mainly used to debugging, only useable in combination with IRAIL
     */
    public class Stations : LinkedObject
    {
        private static readonly Stations Nmbs = new Stations(new Uri("http://irail.be/stations"));

        private readonly Dictionary<Uri, string> _mapping = new Dictionary<Uri, string>();
        private readonly Dictionary<string, Uri> _reverseMapping = new Dictionary<string, Uri>();


        public static readonly Uri BrusselZuid = AsUri("http://irail.be/stations/NMBS/008814001");
        public static readonly Uri Gent = AsUri("https://irail.be/stations/NMBS/008892007");
        public static readonly Uri Brugge = AsUri("https://irail.be/stations/NMBS/008891009");
        public static readonly Uri Poperinge = AsUri("https://irail.be/stations/NMBS/008896735");
        public static readonly Uri Vielsalm = AsUri("https://irail.be/stations/NMBS/008845146");


        private Stations(Uri uri) : base(uri)
        {
            try
            {
                // Download();
            }
            catch (Exception e)
            {
                Log.Warning($"Could not download station list: {e}");
            }
        }


        protected override void FromJson(JToken json)
        {
            foreach (var js in json["@graph"])
            {
                var s = new Station(js);
                _mapping.Add(s.Id(), s.Name);
                _reverseMapping.Add(s.Name, s.Id());
            }
        }

        public static string GetName(Uri uri)
        {
            return Nmbs._mapping == null ? uri.ToString() : Nmbs._mapping.GetValueOrDefault(uri, uri.ToString());
        }

        public static Uri GetId(string name)
        {
            if (!Nmbs._reverseMapping.ContainsKey(name))
            {
                throw new ArgumentException($"The station {name} is not found in the NMBS-list");
            }

            return Nmbs._reverseMapping[name];
        }
    }
}