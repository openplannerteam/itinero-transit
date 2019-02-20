using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// ReSharper disable MemberCanBePrivate.Global

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]

namespace Itinero.Transit.IO.LC.CSA
{
    public static class Belgium
    {
        public static LinkedConnectionDataset Sncb()
        {
            return new LinkedConnectionDataset(
                new Uri("https://graph.irail.be/sncb/connections"),
                new Uri("https://irail.be/stations")
            );
        }


        public static LinkedConnectionDataset DeLijn()
        {
            return new LinkedConnectionDataset(new List<LinkedConnectionDataset>
            {
                WestVlaanderen(),
                OostVlaanderen(),
                VlaamsBrabant(),
                Limburg(),
                Antwerpen()
            });
        }


        private static LinkedConnectionDataset CreateDeLijnProfile(string province)
        {
            return new LinkedConnectionDataset(
                new Uri($"https://openplanner.ilabt.imec.be/delijn/{province}/connections"),
                new Uri($"https://openplanner.ilabt.imec.be/delijn/{province}/stops")
            );
        }

        public static LinkedConnectionDataset WestVlaanderen()
        {
            return CreateDeLijnProfile("West-Vlaanderen");
        }


        public static LinkedConnectionDataset OostVlaanderen()
        {
            return CreateDeLijnProfile("Oost-Vlaanderen");
        }


        public static LinkedConnectionDataset Limburg()
        {
            return CreateDeLijnProfile("Limburg");
        }


        public static LinkedConnectionDataset VlaamsBrabant()
        {
            return CreateDeLijnProfile("Vlaams-Brabant");
        }

        public static LinkedConnectionDataset Antwerpen()
        {
            return CreateDeLijnProfile("Antwerpen");
        }
    }
}