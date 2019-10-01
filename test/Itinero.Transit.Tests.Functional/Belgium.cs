using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Transit.IO.LC;

// ReSharper disable MemberCanBePrivate.Global

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]

namespace Itinero.Transit.Tests.Functional
{
    public static class  Belgium
    {
        /// <summary>
        /// All links for Belgium
        /// All lowercase, e.g. 'delijn-west-vlaanderen'
        /// </summary>
        public static readonly IReadOnlyDictionary<string, (string connections, string locations)> AllLinks = new Dictionary<string, (string connections, string locations)>
        {
            {"sncb", ("https://graph.irail.be/sncb/connections", "https://irail.be/stations")},
         
            {"delijn-west-vlaanderen", ("https://openplanner.ilabt.imec.be/delijn/West-Vlaanderen/connections", "https://openplanner.ilabt.imec.be/delijn/West-Vlaanderen/stops")},
            {"delijn-oost-vlaanderen", ("https://openplanner.ilabt.imec.be/delijn/Oost-Vlaanderen/connections", "https://openplanner.ilabt.imec.be/delijn/Oost-Vlaanderen/stops")},
            {"delijn-limburg", ("https://openplanner.ilabt.imec.be/delijn/Limburg/connections", "https://openplanner.ilabt.imec.be/delijn/Limburg/stops")},
            {"delijn-vlaams-brabant", ("https://openplanner.ilabt.imec.be/delijn/Vlaams-Brabant/connections", "https://openplanner.ilabt.imec.be/delijn/Vlaams-Brabant/stops")},
            {"delijn-antwerpen", ("https://openplanner.ilabt.imec.be/delijn/Antwerpen/connections", "https://openplanner.ilabt.imec.be/delijn/Antwerpen/stops")},
        };

        
        

        public const string SncbConnections = "https://graph.irail.be/sncb/connections";
        public const string SncbLocations = "https://irail.be/stations";

        
        public static LinkedConnectionDataset Sncb()
        {
            return new LinkedConnectionDataset(
                new Uri(SncbConnections),
                new Uri(SncbLocations)
            );
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

        private static LinkedConnectionDataset Antwerpen()
        {
            return CreateDeLijnProfile("Antwerpen");
        }
    }
}
