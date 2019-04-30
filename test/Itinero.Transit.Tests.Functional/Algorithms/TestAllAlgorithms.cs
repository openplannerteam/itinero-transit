using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Tests.Functional.Algorithms.CSA;
using Itinero.Transit.Tests.Functional.IO.LC;
using Itinero.Transit.Tests.Functional.IO.LC.Synchronization;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests.Functional.Algorithms
{
    /// <summary>
    /// Tests a bunch of algorithms and cross-properties which should be honored
    /// All tests are performed against a fixed dataset
    /// </summary>
    public class TestAllAlgorithms : FunctionalTest<bool, (TransitDb, DateTime)>
    {
        private static readonly Dictionary<string, DefaultFunctionalTest> AllTestsNamed =
            new Dictionary<string, DefaultFunctionalTest>
            {
                {"eas", EarliestConnectionScanTest.Default},
                {"las", LatestConnectionScanTest.Default},
                {"pcs", ProfiledConnectionScanTest.Default},
                {"easpcs", EasPcsComparison.Default},
                {"easlas", EasLasComparison.Default},
                {"isochrone", IsochroneTest.Default}
            };

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


        /// <summary>
        ///  Tests all algorithms, with the default test data on the default test date
        /// </summary>
        /// <returns></returns>
        public TransitDb ExecuteDefault(DateTime date)
        {
            var path = $"fixed-test-cases-{date:yyyy-MM-dd}.transitdb";
            var db = TransitDb.ReadFrom(path, 0);
            Execute((db, date));
            return db;
        }

        /// <summary>
        /// Test the algos with data from today.
        /// Might download the data, might reuse a cached version
        /// </summary>
        public void TestLive()
        {
            var date = DateTime.Now;

            TransitDb db;
            var fileN = $"{date:yyyy-MM-dd}";
            var path = $"test-write-to-disk-{fileN}.transitdb";
            try
            {
                db = TransitDb.ReadFrom(path, 0);
                Serilog.Log.Information("Reused already existing tdb for testing");
            }
            catch (Exception e)
            {
                Serilog.Log.Error(e.ToString());
                db = LoadTransitDbTest.Default.Run((date.Date, new TimeSpan(1, 0, 0, 0)));
                new TestWriteToDisk().Run((db, fileN));
            }

            Execute((db, date));
        }


        protected override bool Execute((TransitDb, DateTime) inputData)
        {
            var inputs = CreateInputs(inputData.Item1, inputData.Item2);
            var tests = AllTestsNamed.Select(kv => kv.Value);


            var failed = 0;
            var results = new Dictionary<string, List<int>>();


            void RegisterFail<T>(string name, T input, int i)
            {
                Serilog.Log.Error($"{name} failed on input #{i} {input}");
                failed++;
            }


            foreach (var t in tests)
            {
                var i = 0;
                var name = t.GetType().Name;
                results[name] = new List<int>();
                foreach (var input in inputs)
                {
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
                        Serilog.Log.Error(e.ToString());
                        RegisterFail(name, input, i);
                    }

                    i++;
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

                Serilog.Log.Information($"{name}: {results[name].Count}/{inputs.Count} {fails}");
            }

            if (failed > 0)
            {
                throw new Exception("Some tests failed");
            }

            return true;
        }


        private List<(TransitDb, string, string, DateTime, DateTime)> CreateInputs(TransitDb db, DateTime date)
        {
            return new List<(TransitDb, string, string, DateTime, DateTime)>
            {
                (db, Brugge,
                    Gent,
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)), //*/
                (db, Poperinge, Brugge,
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)), //*/
                (db, Brugge, Poperinge,
                    date.Date.AddHours(9),
                    date.Date.AddHours(12)),
                (db,
                    Oostende,
                    Brugge,
                    date.Date.AddHours(9),
                    date.Date.AddHours(11)),
                (db,
                    Brugge,
                    Oostende,
                    date.Date.AddHours(9),
                    date.Date.AddHours(11)),
                (db,
                    BrusselZuid,
                    Leuven,
                    date.Date.AddHours(9),
                    date.Date.AddHours(14)),
                (db,
                    Leuven,
                    SintJorisWeert,
                    date.Date.AddHours(9),
                    date.Date.AddHours(14)),
                (db,
                    BrusselZuid,
                    SintJorisWeert,
                    date.Date.AddHours(9),
                    date.Date.AddHours(14)),
                (db,
                    Brugge,
                    Kortrijk,
                    date.Date.AddHours(6),
                    date.Date.AddHours(20)),
                (db, Kortrijk,
                    Vielsalm,
                    date.Date.AddHours(9),
                    date.Date.AddHours(18))
            };
        }
    }
}