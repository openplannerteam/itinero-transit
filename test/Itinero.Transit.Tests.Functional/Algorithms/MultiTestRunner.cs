using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.Tests.Functional.Utils;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests.Functional.Algorithms
{
    /// <summary>
    /// Tests a bunch of algorithms and cross-properties which should be honored
    /// All tests are performed against a fixed dataset
    /// </summary>
    public class MultiTestRunner : FunctionalTestWithInput<
        FunctionalTestWithInput<WithTime<TransferMetric>>>
    {
        private List<WithTime<TransferMetric>> _inputs;


        /// <summary>
        /// Constructs an algorithmTester for the given TransitDbs at the given date
        /// </summary>
        public MultiTestRunner(
            IReadOnlyList<string> transitDbs, DateTime date,
            Func<WithProfile<TransferMetric>, DateTime, List<WithTime<TransferMetric>>> createInputs,
            Profile<TransferMetric> profile = null)
        {
            var tdbs = new List<TransitDbSnapShot>();
            for (uint i = 0; i < transitDbs.Count; i++)
            {
                var path = transitDbs[(int) i];
                var tdb = TransitDbCache.Get(path, i).Latest;
                tdbs.Add(tdb);
            }

            profile = profile ?? new DefaultProfile();
            var withProfileProfile = tdbs.SelectProfile(profile);

            _inputs = createInputs(withProfileProfile, date);
        }


        /// <summary>
        /// Creates a tester with NMBS-loaded + all applicable inputs
        /// </summary>
        /// <returns></returns>
        public static MultiTestRunner NmbsOnlyTester()
        {
            return new MultiTestRunner(
                new List<string> {StringConstants.Nmbs}, StringConstants.TestDate, TestConstants.NmbsInputs);
        }


        /// <summary>
        /// Creates a tester with Delijn loaded + all applicable inputs for those tests
        /// </summary>
        /// <returns></returns>
        public static MultiTestRunner DelijnNmbsTester()
        {
            return new MultiTestRunner(
                StringConstants.TestDbs, StringConstants.TestDate,
                (a, b) =>
                {
                    return TestConstants.NmbsInputs(a, b).Concat(TestConstants.MultimodalInputs(a, b)).ToList();
                });
        }


        protected override void Execute()
        {
            // Run the test named 'Input' over all the input cases
            Input.RunOverMultiple(_inputs);
        }

        public void TestSingleInput(int i)
        {
            var input = _inputs[i];
            // Run the test named 'Input'
            Input.RunOverMultiple(new List<WithTime<TransferMetric>> {input});
        }

        /// <summary>
        /// Run all the tests from TestConstants
        /// </summary>
        public void RunAllTests()
        {
            // Run all the tests
            RunOverMultiple(TestConstants.AllAlgorithmicTests);
        }

      
    }
}