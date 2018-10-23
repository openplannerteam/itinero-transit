using System;
using Itinero_Transit.CSA.Connections;
using Itinero_Transit.CSA.Data;
using Itinero_Transit.CSA.LocationProviders;
using Itinero_Transit.LinkedData;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

namespace Itinero_Transit.CSA.ConnectionProviders
{
    public static class Sncb
    {
        public static Profile<TransferStats> Profile(IDocumentLoader loader, LocalStorage storage, string routerdbPath)
        {
            var prov = new LocallyCachedConnectionsProvider(
                new LinkedConnectionProvider(HydraSearch(loader)), storage);
            var loc = Location(storage);
            var footpaths = new TransferGenerator(loc, routerdbPath);
            return new Profile<TransferStats>(prov, loc, footpaths,
                TransferStats.Factory, TransferStats.ProfileCompare, TransferStats.ParetoCompare);
        }

        public static JObject HydraSearch(IDocumentLoader loader)
        {
            var proc =
                new JsonLdProcessor(loader, new Uri("http://graph.irail.be/sncb/connections"));

            var jsonld = proc.LoadExpanded(new Uri("http://graph.irail.be/sncb/connections"));
            return (JObject) jsonld["http://www.w3.org/ns/hydra/core#search"][0];
        }

        private static ILocationProvider Location(LocalStorage storage)
        {
            var uri = new Uri("http://irail.be/stations");
            var proc = new JsonLdProcessor(new Downloader(caching: false), uri);
            return new CachedLocationsFragment(uri, proc, storage);
        }
    }
}