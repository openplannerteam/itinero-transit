using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
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
            TransferMetric.Factory, TransferMetric.ParetoCompare
        );


        private static readonly List<DefaultFunctionalTest<TransferMetric>> AllTests =
            new List<DefaultFunctionalTest<TransferMetric>>
            {
                new EarliestConnectionScanTest(),
                new LatestConnectionScanTest(),
                new ProfiledConnectionScanTest(), //*/
                new EasPcsComparison(),
                new EasLasComparison(),
                new IsochroneTest(),
                //      new ProfiledConnectionScanWithMetricFilteringTest(),
                new MultiTransitDbTest() //*/
            };


        public const string _osmCentrumShuttle =
            "testdata/fixed-test-cases-osm-CentrumbusBrugge2019-07-11.transitdb";

        public const string _nmbs = "testdata/fixed-test-cases-sncb-2019-07-11.transitdb";
        public const string _delijnWvl = "testdata/fixed-test-cases-de-lijn-wvl-2019-07-11.transitdb";


        /// <summary>
        ///  Tests all algorithms, with the default test data on the default test date
        /// </summary>
        /// <returns></returns>
        public TransitDb ExecuteDefault()
        {
            Execute(new List<string> {_nmbs}, Constants.TestDate, CreateInputs, AllTests);
            return tdbCache[_nmbs];
        }


        public void ExecuteMultiModal(int input = -1)
        {
            Execute(Constants.TestDbs, Constants.TestDate,
                a =>
                {
                    var list = CreateInputs(a).Concat(CreateInputsMultiModal(a)).ToList();
                    if (input >= 0)
                    {
                        list = new List<WithTime<TransferMetric>>() {list[input]};
                    }

                    return list;
                },
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
                withProfile.SelectStops(
                    Constants.Brugge, Constants.Gent).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)), //*/
                withProfile.SelectStops(Constants.Poperinge, Constants.Brugge).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)),
                withProfile.SelectStops(Constants.Brugge, Constants.Poperinge).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)),
                withProfile.SelectStops(
                    Constants.Oostende,
                    Constants.Brugge).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(11)),
                withProfile.SelectStops(
                    Constants.Brugge,
                    Constants.Oostende).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(11)),
                withProfile.SelectStops(
                    Constants.BrusselZuid,
                    Constants.Leuven).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(14)),
                withProfile.SelectStops(
                    Constants.Leuven,
                    Constants.SintJorisWeert).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(14)),
                withProfile.SelectStops(
                    Constants.BrusselZuid,
                    Constants.SintJorisWeert).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(14)),
                withProfile.SelectStops(
                    Constants.Brugge,
                    Constants.Kortrijk).SelectTimeFrame(
                    date.Date.AddHours(10),
                    date.Date.AddHours(12)),
                withProfile.SelectStops(
                    Constants.Kortrijk,
                    Constants.Vielsalm).SelectTimeFrame(
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
                withProfile.SelectStops(Constants.CoiseauKaaiOsm,
                    Constants.Gent).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)),
                withProfile.SelectStops(Constants.CoiseauKaaiOsm,
                    Constants.GentZwijnaardeDeLijn).SelectTimeFrame(
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)),
            };
        }
    }
}