using System;
using System.Collections.Generic;
using Itinero_Transit.CSA.ConnectionProviders;
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

        private Stations(Uri uri) : base(uri)
        {
            try
            {
                Download(SncbConnectionProvider.IrailProcessor);
            }
            catch (Exception e)
            {
                Log.Warning($"Could not download station list: {e}");
            }
        }


        protected override void FromJson(JObject json)
        {
            foreach (var js in json["@graph"])
            {
                var s = new Station((JObject) js);
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