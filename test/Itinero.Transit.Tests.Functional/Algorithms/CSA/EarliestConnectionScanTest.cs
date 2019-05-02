using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using NetTopologySuite.Utilities;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class EarliestConnectionScanTest : DefaultFunctionalTest<TransferMetric>
    {
        public static EarliestConnectionScanTest Default => new EarliestConnectionScanTest();

        protected override bool Execute(WithTime<TransferMetric> input)
        {
            var journey = input.EarliestArrivalJourney();
            NotNull(journey);
            return true;
        }
    }
}