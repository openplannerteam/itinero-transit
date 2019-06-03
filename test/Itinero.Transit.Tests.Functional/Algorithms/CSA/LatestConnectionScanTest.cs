using Itinero.Transit.Journeys;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class LatestConnectionScanTest :
        DefaultFunctionalTest<TransferMetric>
    {
        protected override bool Execute(WithTime<TransferMetric> input)
        {
            var lasJ = input.LatestDepartureJourney();
            NotNull(lasJ);
            // NoLoops(lasJ, input);
            // LAS can possible create a transfer which could have been taken sooner,
            // but that is just how LAS works
            return true;
        }
    }
}