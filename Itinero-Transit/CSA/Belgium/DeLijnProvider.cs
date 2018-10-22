using System;
using System.Collections.Generic;
using Itinero_Transit.CSA.ConnectionProviders.LinkedConnection;
using Itinero_Transit.CSA.LocationProviders;
using Itinero_Transit.LinkedData;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

namespace Itinero_Transit.CSA.ConnectionProviders
{
    public class DeLijnProvider : LinkedConnectionProvider
    {
        public static Uri StopLocationsRoot = new Uri("http://dexagod.github.io/stoplocations/t0.jsonld");
        public static Uri StopFragmentsRoot = new Uri("http://dexagod.github.io/stopsdata/d6.jsonld");

        public static List<Uri> Provinces = new List<Uri>
        {
            new Uri("https://belgium.linkedconnections.org/delijn/Antwerpen/stops"),
            new Uri("https://belgium.linkedconnections.org/delijn/Oost-Vlaanderen/stops"),
            new Uri("https://belgium.linkedconnections.org/delijn/West-Vlaanderen/stops"),
            new Uri("https://belgium.linkedconnections.org/delijn/Vlaams-Brabant/stops"),
            new Uri("https://belgium.linkedconnections.org/delijn/Limburg/stops")
        };

        public static Uri Wvl =
            new Uri(
                "https://belgium.linkedconnections.org/delijn/West-Vlaanderen/connections?departureTime=2018-10-18T10:25:00.000Z");


        public DeLijnProvider(Downloader loader) : base(Hydra(loader), LocationProvider(loader))
        {
        }

        private static JObject Hydra(Downloader loader)
        {
            var proc =
                new JsonLdProcessor(loader, new Uri("http://graph.irail.be/sncb/connections"));

            var jsonld = proc.LoadExpanded(Wvl);
            return (JObject) jsonld["http://www.w3.org/ns/hydra/core#search"][0];
        }

        public static ILocationProvider LocationProvider(Downloader loader)
        {
            var locations = new List<ILocationProvider>();
            foreach (var prov in Provinces)
            {
                var lf = new LocationsFragment(prov);
                lf.Download(new JsonLdProcessor(loader, prov));
                locations.Add(lf);
            }
            
            //var locations = new RdfTreeTraverser(StopLocationsRoot,
            //  new JsonLdProcessor(loader, StopLocationsRoot),
            // new JsonLdProcessor(loader, StopFragmentsRoot));
            return new LocationCombiner(locations);
        }
        
        
    }
}