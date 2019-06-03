using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Itinero.Transit.Tests.Functional.Algorithms.CSA;
using Serilog;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests.Functional.Algorithms
{
    /// <summary>
    /// Tests a bunch of algorithms and cross-properties which should be honored
    /// All tests are performed against a fixed dataset
    /// </summary>
    public class TestAllAlgorithms
    {
        private Profile<TransferMetric> Profile = new Profile<TransferMetric>(new InternalTransferGenerator(),
            new CrowsFlightTransferGenerator(),
            TransferMetric.Factory, TransferMetric.ProfileTransferCompare
        );


        private static readonly List<DefaultFunctionalTest<TransferMetric>> AllTests =
            new List<DefaultFunctionalTest<TransferMetric>>
            {
                new EarliestConnectionScanTest(),
                new LatestConnectionScanTest(),
                new ProfiledConnectionScanTest(),
                new EasPcsComparison(),
                new EasLasComparison(),
                new IsochroneTest(),
                new MultiTransitDbTest() 
            };



        public static readonly IReadOnlyList<string> testDbs = new[]
        {
            _nmbs,
            _osmCentrumShuttle,
            _delijnVlB,
            _delijnWvl,
            _delijnOVl,
            _delijnLim,
            _delijnAnt
        };


        public const string _osmCentrumShuttle = "testdata/fixed-test-cases-osm-CentrumbusBrugge2019-05-30.transitdb";
        public const string _nmbs =      "testdata/fixed-test-cases-sncb-2019-05-30.transitdb";
        public const string _delijnWvl = "testdata/fixed-test-cases-de-lijn-wvl-2019-05-30.transitdb";
        public const string _delijnOVl = "testdata/fixed-test-cases-de-lijn-ovl-2019-05-30.transitdb";
        public const string _delijnVlB = "testdata/fixed-test-cases-de-lijn-vlb-2019-05-30.transitdb";
        public const string _delijnLim = "testdata/fixed-test-cases-de-lijn-lim-2019-05-30.transitdb";
        public const string _delijnAnt = "testdata/fixed-test-cases-de-lijn-ant-2019-05-30.transitdb";

        
        public static DateTime TestDate = new DateTime(2019,05,30, 09,00,00).ToUniversalTime().Date;

        public const string Gent = "http://irail.be/stations/NMBS/008892007";
        public const string Brugge = "http://irail.be/stations/NMBS/008891009";
        public const string Poperinge = "http://irail.be/stations/NMBS/008896735";
        public const string Vielsalm = "http://irail.be/stations/NMBS/008845146";
        public const string BrusselZuid = "http://irail.be/stations/NMBS/008814001";
        public const string Kortrijk = "http://irail.be/stations/NMBS/008896008";
        public const string Oostende = "http://irail.be/stations/NMBS/008891702";
        public const string Antwerpen = "http://irail.be/stations/NMBS/008821006"; // Antwerpen centraal
        public const string SintJorisWeert = "http://irail.be/stations/NMBS/008833159"; // Antwerpen centraal
        public const string Leuven = "http://irail.be/stations/NMBS/008833001"; // Antwerpen centraal
        public const string Howest = "https://data.delijn.be/stops/502132";
        public const string ZandStraat = "https://data.delijn.be/stops/500562";
        public const string AzSintJan = "https://data.delijn.be/stops/502083";
        public const string Moereind = "https://data.delijn.be/stops/107455";

        public const string GentZwijnaardeDeLijn = "https://data.delijn.be/stops/200657";

       public const string StationBruggeOsm = "https://www.openstreetmap.org/node/6348496391";
       public const string CoiseauKaaiOsm = "https://www.openstreetmap.org/node/6348562147";


        /// <summary>
        ///  Tests all algorithms, with the default test data on the default test date
        /// </summary>
        /// <returns></returns>
        public TransitDb ExecuteDefault()
        {
            Execute(new List<string> {_nmbs}, TestDate, CreateInputs, AllTests);
            return tdbCache[_nmbs];
        }


        public void ExecuteMultiModal()
        {
            Execute(testDbs, TestDate,
                a => CreateInputs(a).Concat(CreateInputsMultiModal(a)).ToList(),
                AllTests);
        }


        private readonly Dictionary<string, TransitDb> tdbCache = new Dictionary<string, TransitDb>();

        private void Execute(IReadOnlyList<string> dbs, DateTime date,
            Func<(IEnumerable<TransitDb.TransitDbSnapShot>, DateTime), List<WithTime<TransferMetric>>> createInputs,
            IEnumerable<DefaultFunctionalTest<TransferMetric>> tests
        )
        {
            var tdbs = new List<TransitDb.TransitDbSnapShot>();
            for (uint i = 0; i < dbs.Count; i++)
            {
                var path = dbs[(int) i];
                if (!tdbCache.ContainsKey(path))
                {
                    tdbCache[path] = TransitDb.ReadFrom(path, i);
                }

                tdbs.Add(tdbCache[path].Latest);
            }

            var inputs = createInputs((tdbs, date));


            var failed = 0;
            var results = new Dictionary<string, List<int>>();


            void RegisterFail<T>(string name, T input, int i)
            {
                Log.Error($"{name} failed on input #{i} {input}");
                failed++;
            }


            foreach (var t in tests)
            {
                var name = t.GetType().Name;
                results[name] = new List<int>();


                foreach (var input in inputs)
                {
                    var i = inputs.IndexOf(input);
                    input.ResetFilter();
                    try
                    {
                        if (!t.RunPerformance(input))
                        {
                            RegisterFail(name, input, i);
                        }
                        else
                        {
                            results[name].Add(i);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());
                        RegisterFail(name, input, i);
                    }
                }
            }

            foreach (var t in tests)
            {
                var name = t.GetType().Name;
                var fails = "";
                for (var j = 0; j < inputs.Count; j++)
                {
                    if (!results[name].Contains(j))
                    {
                        fails += $"{j}, ";
                    }
                }

                if (!string.IsNullOrEmpty(fails))
                {
                    fails = "Failed: " + fails;
                }

                Log.Information($"{name}: {results[name].Count}/{inputs.Count} {fails}");
            }

            if (failed > 0)
            {
                throw new Exception("Some tests failed");
            }
        }

        private static List<WithTime<TransferMetric>> CreateInputs((IEnumerable<TransitDb.TransitDbSnapShot>,
            DateTime) arg)
        {
            var db = arg.Item1;
            var date = arg.Item2;
            var withProfile = db.SelectProfile(new DefaultProfile()).PrecalculateClosestStops();


            return new List<WithTime<TransferMetric>>
            {
                withProfile.SelectStops(Brugge, Gent).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)), //*/
                withProfile.SelectStops(Poperinge, Brugge).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)),
                withProfile.SelectStops(Brugge, Poperinge).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)),
                withProfile.SelectStops(
                    Oostende,
                    Brugge).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(11)),
                withProfile.SelectStops(
                    Brugge,
                    Oostende).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(11)),
                withProfile.SelectStops(
                    BrusselZuid,
                    Leuven).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(14)),
                withProfile.SelectStops(
                    Leuven,
                    SintJorisWeert).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(14)),
                withProfile.SelectStops(
                    BrusselZuid,
                    SintJorisWeert).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(14)),
                withProfile.SelectStops(
                    Brugge,
                    Kortrijk).SelectTimeFrame(
                    date.Date.AddHours(10),
                    date.Date.AddHours(12)),
                withProfile.SelectStops(Kortrijk,
                    Vielsalm).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(18)) //*/
            };
        }


        /// <summary>
        ///  Test cases over multiple operators
        /// </summary>
        /// <returns></returns>
        private static List<WithTime<TransferMetric>> CreateInputsMultiModal(
            ( IEnumerable<TransitDb.TransitDbSnapShot> db,
                DateTime date) arg)
        {
            var db = arg.db;
            var date = arg.date;
            var withProfile = db.SelectProfile(new DefaultProfile()).PrecalculateClosestStops();

            return new List<WithTime<TransferMetric>>
            {
                withProfile.SelectStops(CoiseauKaaiOsm,
                    Gent).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)),
                withProfile.SelectStops(CoiseauKaaiOsm,
                    GentZwijnaardeDeLijn).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)),
            };
        }
    }
}