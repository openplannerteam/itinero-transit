using System;
using Itinero.Transit.CSA.Connections;
using Itinero.Transit.CSA.Data;
using Itinero.Transit.CSA.LocationProviders;
using Itinero.Transit.LinkedData;
using JsonLD.Core;

namespace Itinero.Transit.CSA.ConnectionProviders
{
    public static class Sncb
    {
        public static Profile<TransferStats> Profile(string storageLocation, string routerdbPath)
        {
            storageLocation = storageLocation + "/sncb";
            var prov = new LocallyCachedConnectionsProvider(
                new LinkedConnectionProvider(new Uri("http://graph.irail.be/sncb/"), "https://graph.irail.be/sncb/connections{?departureTime}"
                    ), new LocalStorage(storageLocation+"/timeTables"));
            var loc = Location(new LocalStorage(storageLocation));
            var footpaths = new TransferGenerator(routerdbPath);
            return new Profile<TransferStats>(prov, loc, footpaths,
                TransferStats.Factory, TransferStats.ProfileCompare, TransferStats.ParetoCompare);
        }

        private static ILocationProvider Location(LocalStorage storage)
        {
            var uri = new Uri("http://irail.be/stations");
            var proc = new JsonLdProcessor(new Downloader(caching: false), uri);
            return new CachedLocationsFragment(uri, proc, storage);
        }
    }
}