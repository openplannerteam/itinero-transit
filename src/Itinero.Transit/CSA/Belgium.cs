using System;
using System.Collections.Generic;

namespace Itinero.Transit
{
    public class Belgium
    {
        
        public static Profile<TransferStats> Sncb(LocalStorage storage)
        {
            return new Profile<TransferStats>(
                "SNCB",
                new Uri("http://graph.irail.be/sncb/connections"),
                new Uri("http://irail.be/stations"),
                "belgium.routerdb",
                storage,
                TransferStats.Factory, 
                TransferStats.ProfileTransferCompare,
                TransferStats.ParetoCompare
            );
        }


        public static Profile<TransferStats> DeLijn(LocalStorage storage)
        {
            var profs = new List<Profile<TransferStats>>
            {
                WestVlaanderen(storage),
                OostVlaanderen(storage),
                VlaamsBrabant(storage),
                Limburg(storage),
                Antwerpen(storage)
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
        
        
        private static Profile<TransferStats> CreateDeLijnProfile(string province, LocalStorage storage)
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
                TransferStats.ParetoCompare
            );
        }

        public static Profile<TransferStats> WestVlaanderen(LocalStorage storage)
        {
            return CreateDeLijnProfile("West-Vlaanderen", storage);
        }
        
        
        public static Profile<TransferStats> OostVlaanderen(LocalStorage storage)
        {
            return CreateDeLijnProfile("Oost-Vlaanderen", storage);
        }
        
        
        public static Profile<TransferStats> Limburg(LocalStorage storage)
        {
            return CreateDeLijnProfile("Limburg", storage);
        }
        
        
        public static Profile<TransferStats> VlaamsBrabant(LocalStorage storage)
        {
            return CreateDeLijnProfile("Vlaams-Brabant", storage);
        }
        
        public static Profile<TransferStats> Antwerpen(LocalStorage storage)
        {
            return CreateDeLijnProfile("Antwerpen", storage);
        }

      
    }
}