using System;
using System.Collections.Generic;

namespace Itinero.Transit
{
    public class Belgium
    {

        public static Profile<TransferStats> Sncb(LocalStorage storage, Downloader loader = null)
        {
            return new Profile<TransferStats>(
                "SNCB",
                new Uri("http://graph.irail.be/sncb/connections"),
                new Uri("http://irail.be/stations"),
                "belgium.routerdb",
                storage,
                TransferStats.Factory, 
                TransferStats.ProfileTransferCompare,
                TransferStats.ParetoCompare,
                loader
            );
        }


        public static Profile<TransferStats> DeLijn(LocalStorage storage, Downloader loader = null)
        {
            var profs = new List<Profile<TransferStats>>
            {
                WestVlaanderen(storage, loader),
                OostVlaanderen(storage, loader),
                VlaamsBrabant(storage, loader),
                Limburg(storage, loader),
                Antwerpen(storage, loader)
            };

            var conn = new List<IConnectionsProvider>();
            var locs = new List<ILocationProvider>();
            foreach (var prof in profs)
            {
                conn.Add(prof);
                locs.Add(prof);
            }
            locs.Add(OsmLocationMapping.Singleton);
            return new Profile<TransferStats>(
                new ConnectionProviderMerger(conn), 
                new LocationCombiner(locs), 
                profs[0].FootpathTransferGenerator,
                TransferStats.Factory, 
                TransferStats.ProfileTransferCompare,
                TransferStats.ParetoCompare
                );


        }
        
        
        private static Profile<TransferStats> CreateDeLijnProfile(string province, LocalStorage storage,
            Downloader loader)
        {
            storage = storage.SubStorage("DeLijn");
            
            return new Profile<TransferStats>(
                "DeLijnWvl",
                new Uri($"http://openplanner.ilabt.imec.be/delijn/{province}/connections"),
                new Uri($"http://openplanner.ilabt.imec.be/delijn/{province}/stops"),
                "belgium.routerdb",
                storage,
                TransferStats.Factory, 
                TransferStats.ProfileCompare,
                TransferStats.ParetoCompare,
                loader
            );
        }

        public static Profile<TransferStats> WestVlaanderen(LocalStorage storage, Downloader loader)
        {
            return CreateDeLijnProfile("West-Vlaanderen", storage, loader);
        }
        
        
        public static Profile<TransferStats> OostVlaanderen(LocalStorage storage, Downloader loader)
        {
            return CreateDeLijnProfile("Oost-Vlaanderen", storage, loader);
        }
        
        
        public static Profile<TransferStats> Limburg(LocalStorage storage, Downloader loader)
        {
            return CreateDeLijnProfile("Limburg", storage, loader);
        }
        
        
        public static Profile<TransferStats> VlaamsBrabant(LocalStorage storage, Downloader loader)
        {
            return CreateDeLijnProfile("Vlaams-Brabant", storage, loader);
        }
        
        public static Profile<TransferStats> Antwerpen(LocalStorage storage, Downloader loader)
        {
            return CreateDeLijnProfile("Antwerpen", storage, loader);
        }

      
    }
}