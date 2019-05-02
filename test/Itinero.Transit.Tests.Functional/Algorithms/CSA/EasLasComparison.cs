using System;
using System.Collections.Generic;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    /// <summary>
    /// When running PCS (without pruning), the earliest route should equal the one calculated by EAS.
    /// If not  something is wrong
    /// </summary>
    public class EasLasComparison : DefaultFunctionalTest<TransferMetric>
    {
        protected override bool Execute(WithTime<TransferMetric> input)
        {
            var easJ =
                input.EarliestArrivalJourney();

            NotNull(easJ);

            var lasJ =
                input.DifferentTimes(input.Start, easJ.ArrivalTime().FromUnixTime())
                    .LatestDepartureJourney();

            NotNull(lasJ);

            // Eas is bound by the first departing train, while las is not
            True(easJ.Root.DepartureTime() <= lasJ.Root.DepartureTime());
            True(easJ.ArrivalTime() >= lasJ.ArrivalTime());


            return true;
        }
    }
}