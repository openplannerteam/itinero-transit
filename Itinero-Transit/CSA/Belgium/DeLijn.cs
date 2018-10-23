using System;
using System.Collections.Generic;
using Itinero_Transit.CSA.Connections;
using Itinero_Transit.CSA.Data;
using Itinero_Transit.CSA.LocationProviders;
using Itinero_Transit.LinkedData;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

namespace Itinero_Transit.CSA.ConnectionProviders
{
    public static class DeLijn
    {
        public static List<Uri> ProvincesLocations = new List<Uri>
        {
            new Uri("https://belgium.linkedconnections.org/delijn/Antwerpen/stops"),
            new Uri("https://belgium.linkedconnections.org/delijn/Oost-Vlaanderen/stops"),
            new Uri("https://belgium.linkedconnections.org/delijn/West-Vlaanderen/stops"),
            new Uri("https://belgium.linkedconnections.org/delijn/Vlaams-Brabant/stops"),
            new Uri("https://belgium.linkedconnections.org/delijn/Limburg/stops")
        };

        public static Uri Wvl =
            new Uri(
                "https://belgium.linkedconnections.org/delijn/West-Vlaanderen/connections");


        public static Profile<TransferStats> Profile(IDocumentLoader loader,
            LocalStorage storage, string routerdbPath)
        {
            var conns = new LocallyCachedConnectionsProvider(new LinkedConnectionProvider(Hydra(loader)), storage);
            var loc = LocationProvider(loader, storage);
            var footpath = new TransferGenerator(loc, routerdbPath);

            return new Profile<TransferStats>(conns, loc, footpath,
                TransferStats.Factory, TransferStats.ProfileCompare, TransferStats.ParetoCompare);
        }
       

        private static JObject Hydra(IDocumentLoader loader)
        {
            var proc =
                new JsonLdProcessor(loader, new Uri("http://graph.irail.be/sncb/connections"));

            var jsonld = proc.LoadExpanded(Wvl);
            return (JObject) jsonld["http://www.w3.org/ns/hydra/core#search"][0];
        }

        public static ILocationProvider LocationProvider(IDocumentLoader loader, LocalStorage storage)
        {
            var locations = new List<ILocationProvider>();
            foreach (var prov in ProvincesLocations)
            {
                var proc = new JsonLdProcessor(loader, prov);
                var lf = new CachedLocationsFragment(prov, proc, storage);
                locations.Add(lf);
            }
            
            return new LocationCombiner(locations);
        }
        
        
    }
}