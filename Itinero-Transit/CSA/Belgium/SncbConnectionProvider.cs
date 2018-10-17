using System;
using Itinero_Transit.CSA.ConnectionProviders.LinkedConnection;
using Itinero_Transit.LinkedData;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

namespace Itinero_Transit.CSA.ConnectionProviders
{
    public class SncbConnectionProvider : LinkedConnectionProvider
    {
        public SncbConnectionProvider() : base(HydraSearch(), Location())
        {
        }

        private static JObject HydraSearch()
        {
            var proc =
                new JsonLdProcessor(new Downloader(), new Uri("http://graph.irail.be/sncb/connections"));

            var jsonld = proc.LoadExpanded(new Uri("http://graph.irail.be/sncb/connections"));
            return (JObject) jsonld["http://www.w3.org/ns/hydra/core#search"][0];
        }

        private static ILocationProvider Location()
        {
            var uri = new Uri("http://irail.be/stations");
            var proc = new JsonLdProcessor(new Downloader(caching:false), uri);
            var dump = new LocationsFragment(uri);
            dump.Download(proc);
            return dump;
        }
    }
}