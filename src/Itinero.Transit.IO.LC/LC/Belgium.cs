using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Transit.IO.LC.CSA.ConnectionProviders;
using Itinero.Transit.IO.LC.CSA.LocationProviders;
using Itinero.Transit.IO.LC.CSA.Utils;

// ReSharper disable MemberCanBePrivate.Global

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]

namespace Itinero.Transit.IO.LC.CSA
{
    public static class Belgium
    {
        public static Profile Sncb(Downloader loader = null)
        {
            return new Profile(new Uri("https://graph.irail.be/sncb/connections"),
                new Uri("https://irail.be/stations"),
                loader
            );
        }


        public static Profile DeLijn(Downloader loader = null)
        {
            return new Profile(new List<Profile>
            {
                WestVlaanderen(loader),
                OostVlaanderen(loader),
                VlaamsBrabant(loader),
                Limburg(loader),
                Antwerpen(loader)
            });
        }


        private static Profile CreateDeLijnProfile(string province,
            Downloader loader)
        {
            return new Profile(new Uri($"https://openplanner.ilabt.imec.be/delijn/{province}/connections"),
                new Uri($"https://openplanner.ilabt.imec.be/delijn/{province}/stops"),
                loader
            );
        }

        public static Profile WestVlaanderen(
            Downloader loader)
        {
            return CreateDeLijnProfile("West-Vlaanderen", loader);
        }


        public static Profile OostVlaanderen(Downloader loader)
        {
            return CreateDeLijnProfile("Oost-Vlaanderen", loader);
        }


        public static Profile Limburg(Downloader loader)
        {
            return CreateDeLijnProfile("Limburg", loader);
        }


        public static Profile VlaamsBrabant(Downloader loader)
        {
            return CreateDeLijnProfile("Vlaams-Brabant", loader);
        }

        public static Profile Antwerpen(Downloader loader)
        {
            return CreateDeLijnProfile("Antwerpen", loader);
        }
    }
}