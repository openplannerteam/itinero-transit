using System;
using System.Collections.Generic;

namespace Itinero.Transit.Tests.Functional.Utils
{
    public static class StringConstants
    {
        public static DateTime TestDate = new DateTime(2019, 07, 11, 09, 00, 00).ToUniversalTime().Date;


        public const string OsmNearStationBruggeLatLon = "https://www.openstreetmap.org/#map=19/51.19764/3.21847";

        public const string OsmNearStationBruggeLatLonRijselse =
            "https://www.openstreetmap.org/#map=17/51.19335/3.21378";

        public const string OsmBruggeHome = "https://www.openstreetmap.org/#map=19/51.21576/3.22048";
        public const string OsmDeSterre = "https://www.openstreetmap.org/#map=18/51.02260/3.71108";
        public const string OsmHermanTeirlinck = "https://www.openstreetmap.org/#map=18/50.86621/4.35104";
        public const string OsmWechel = "https://www.openstreetmap.org/#map=19/51.26290/4.80125";
            
        public const string OsmTielen =    "https://www.openstreetmap.org/#map=19/51.244726870748565/4.895057698232279";
        public const string OsmHerentals =    "https://www.openstreetmap.org/#map=19/51.179504427782405/4.827641884654639";
        

        public const string ZandStraat = "https://data.delijn.be/stops/500562";
        public const string AzSintJan = "https://data.delijn.be/stops/502083";
        public const string Moereind = "https://data.delijn.be/stops/107455";
        public const string GentZwijnaardeDeLijn = "https://data.delijn.be/stops/200657";
        public const string Howest = "https://data.delijn.be/stops/502132";


        public const string Brugge = "http://irail.be/stations/NMBS/008891009";
        public const string Poperinge = "http://irail.be/stations/NMBS/008896735";
        public const string Vielsalm = "http://irail.be/stations/NMBS/008845146";
        public const string BrusselZuid = "http://irail.be/stations/NMBS/008814001";
        public const string Kortrijk = "http://irail.be/stations/NMBS/008896008";
        public const string Oostende = "http://irail.be/stations/NMBS/008891702";
        public const string SintJorisWeert = "http://irail.be/stations/NMBS/008833159"; 
        public const string Leuven = "http://irail.be/stations/NMBS/008833001";
        public const string Gent = "http://irail.be/stations/NMBS/008892007";
        public const string Antwerpen = "http://irail.be/stations/NMBS/008821006"; 
        public const string Aywaille = "http://irail.be/stations/NMBS/008842754";
        public const string Florenville = "http://irail.be/stations/NMBS/008866845";


        public const string CoiseauKaaiOsmNode = "https://www.openstreetmap.org/node/6348562147";
        public const string StationBruggeOsmNode = "https://www.openstreetmap.org/node/6348496391";


        public const string OsmCentrumShuttle = "testdata/fixed-test-cases-osm-CentrumbusBrugge2019-07-11.transitdb";
        public const string Nmbs = "testdata/fixed-test-cases-sncb-2019-07-11.transitdb";
        
        public const string NmbsLongPeriod = "testdata/nmbs-long.transitdb";
        public static readonly DateTime NmbsLongPeriodDate = new DateTime(2019, 07, 13);

        
        public const string DelijnWvl = "testdata/fixed-test-cases-de-lijn-wvl-2019-07-11.transitdb";
        public const string DelijnOVl = "testdata/fixed-test-cases-de-lijn-ovl-2019-07-11.transitdb";
        public const string DelijnVlB = "testdata/fixed-test-cases-de-lijn-vlb-2019-07-11.transitdb";
        public const string DelijnLim = "testdata/fixed-test-cases-de-lijn-lim-2019-07-11.transitdb";
        public const string DelijnAnt = "testdata/fixed-test-cases-de-lijn-ant-2019-07-11.transitdb";


        public static readonly IReadOnlyList<string> TestDbs = new[]
        {
            Nmbs,
            OsmCentrumShuttle,
            DelijnVlB,
            DelijnWvl,
            DelijnOVl,
            DelijnLim,
            DelijnAnt
        };
    }
}