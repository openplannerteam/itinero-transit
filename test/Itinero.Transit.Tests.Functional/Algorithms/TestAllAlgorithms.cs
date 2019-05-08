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
                new EasPcsComparison(), //*/
                new EasLasComparison(),
                new IsochroneTest(),
                new MultiTransitDbTest() //*/
            };

        private static readonly List<DefaultFunctionalTest<TransferMetric>> MultiModalTests =
            new List<DefaultFunctionalTest<TransferMetric>>
            {
                new MultiTransitDbTest()
            };


        private readonly IReadOnlyList<string> testDbs0409 = new[] {_osmCentrumShuttle0409, _nmbs0429};

        public static readonly IReadOnlyList<string> testDbs0429 = new[]
        {
            _nmbs0429,
            _osmCentrumShuttle0429,
            _delijnVlB0429,
            _delijnWvl0429,
            _delijnOVl0429,
            _delijnLim0429,
            _delijnAnt0429,
        };

        public const string _osmCentrumShuttle0409 = "CentrumbusBrugge2019-04-09.transitdb";
        public const string _nmbs0409 = "fixed-test-cases-2019-04-09.transitdb";

        public const string _osmCentrumShuttle0429 = "CentrumbusBrugge2019-04-29.transitdb";
        public const string _nmbs0429 = "fixed-test-cases-2019-04-29.transitdb";
        public const string _delijnWvl0429 = "fixed-test-cases-de-lijn-wvl-2019-04-29.transitdb";
        public const string _delijnOVl0429 = "fixed-test-cases-de-lijn-ovl-2019-04-29.transitdb";
        public const string _delijnVlB0429 = "fixed-test-cases-de-lijn-vlb-2019-04-29.transitdb";
        public const string _delijnLim0429 = "fixed-test-cases-de-lijn-lim-2019-04-29.transitdb";
        public const string _delijnAnt0429 = "fixed-test-cases-de-lijn-ant-2019-04-29.transitdb";


        private const string Gent = "http://irail.be/stations/NMBS/008892007";
        private const string Brugge = "http://irail.be/stations/NMBS/008891009";
        private const string Poperinge = "http://irail.be/stations/NMBS/008896735";
        private const string Vielsalm = "http://irail.be/stations/NMBS/008845146";
        private const string BrusselZuid = "http://irail.be/stations/NMBS/008814001";
        private const string Kortrijk = "http://irail.be/stations/NMBS/008896008";
        private const string Oostende = "http://irail.be/stations/NMBS/008891702";
        private const string Antwerpen = "http://irail.be/stations/NMBS/008821006"; // Antwerpen centraal
        private const string SintJorisWeert = "http://irail.be/stations/NMBS/008833159"; // Antwerpen centraal
        private const string Leuven = "http://irail.be/stations/NMBS/008833001"; // Antwerpen centraal
        private const string Howest = "https://data.delijn.be/stops/502132";
        private const string ZandStraat = "https://data.delijn.be/stops/500562";
        private const string AzSintJan = "https://data.delijn.be/stops/502083";

        private const string GentZwijnaardeDeLijn = "https://data.delijn.be/stops/200657";

        private const string StationBruggeOsm = "https://www.openstreetmap.org/node/6348496391";
        private const string CoiseauKaaiOsm = "https://www.openstreetmap.org/node/6348562147";


        /// <summary>
        ///  Tests all algorithms, with the default test data on the default test date
        /// </summary>
        /// <returns></returns>
        public TransitDb ExecuteDefault()
        {
            var date = new DateTime(2019, 04, 29);
            Execute(new List<string> {_nmbs0429}, date, CreateInputs, AllTests);
            return tdbCache[_nmbs0429];
        }


        public void ExecuteMultiModal()
        {
            Execute(testDbs0429, new DateTime(2019, 04, 29),
                a => CreateInputs(a).Concat(CreateInputsMultiModal(a)).ToList(),
                AllTests.Concat(MultiModalTests));
        }


        private Dictionary<string, TransitDb> tdbCache = new Dictionary<string, TransitDb>();

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