using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.IO.LC.CSA.Utils;

// ReSharper disable MemberCanBePrivate.Global

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]

namespace Itinero.Transit.IO.LC.CSA
{
    public static class Belgium
    {
        public static LinkedConnectionDataset Sncb(Downloader loader = null)
        {
            return new LinkedConnectionDataset(new Uri("https://graph.irail.be/sncb/connections"),
                new Uri("https://irail.be/stations"),
                loader
            );
            
        }


        public static LinkedConnectionDataset DeLijn(Downloader loader = null)
        {
            return new LinkedConnectionDataset(new List<LinkedConnectionDataset>
            {
                WestVlaanderen(loader),
                OostVlaanderen(loader),
                VlaamsBrabant(loader),
                Limburg(loader),
                Antwerpen(loader)
            });
        }


        private static LinkedConnectionDataset CreateDeLijnProfile(string province,
            Downloader loader)
        {
            return new LinkedConnectionDataset(new Uri($"https://openplanner.ilabt.imec.be/delijn/{province}/connections"),
                new Uri($"https://openplanner.ilabt.imec.be/delijn/{province}/stops"),
                loader
            );
        }

        public static LinkedConnectionDataset WestVlaanderen(
            Downloader loader)
        {
            return CreateDeLijnProfile("West-Vlaanderen", loader);
        }


        public static LinkedConnectionDataset OostVlaanderen(Downloader loader)
        {
            return CreateDeLijnProfile("Oost-Vlaanderen", loader);
        }


        public static LinkedConnectionDataset Limburg(Downloader loader)
        {
            return CreateDeLijnProfile("Limburg", loader);
        }


        public static LinkedConnectionDataset VlaamsBrabant(Downloader loader)
        {
            return CreateDeLijnProfile("Vlaams-Brabant", loader);
        }

        public static LinkedConnectionDataset Antwerpen(Downloader loader)
        {
            return CreateDeLijnProfile("Antwerpen", loader);
        }
    }
}