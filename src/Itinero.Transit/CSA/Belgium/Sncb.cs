using System;
using JsonLD.Core;

namespace Itinero.Transit.Belgium
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
                TransferStats.Factory, TransferStats.ProfileTransferCompare, TransferStats.ParetoCompare);
        }

        private static ILocationProvider Location(LocalStorage storage)
        {
            var uri = new Uri("http://irail.be/stations");
            // ReSharper disable once ArgumentsStyleLiteral
            // ReSharper disable once RedundantArgumentDefaultValue
            var proc = new JsonLdProcessor(new Downloader(caching: false), uri);
            return new CachedLocationsFragment(uri, proc, storage);
        }
    }
}