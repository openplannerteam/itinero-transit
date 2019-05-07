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