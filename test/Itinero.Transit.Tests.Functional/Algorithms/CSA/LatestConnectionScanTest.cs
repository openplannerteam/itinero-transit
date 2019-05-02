using System;
using System.Collections.Generic;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class LatestConnectionScanTest :
        DefaultFunctionalTest<TransferMetric>
    {
        public static LatestConnectionScanTest Default => new LatestConnectionScanTest();

        protected override bool Execute(WithTime<TransferMetric> input)
        {
            NotNull(input.LatestDepartureJourney());
            return true;
        }

    }
}