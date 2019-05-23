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
            NoLoops(lasJ, input.StopsReader);
            return true;
        }
    }
}