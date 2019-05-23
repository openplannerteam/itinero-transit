using Itinero.Transit.Journeys;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class EarliestConnectionScanTest : DefaultFunctionalTest<TransferMetric>
    {
        public static EarliestConnectionScanTest Default => new EarliestConnectionScanTest();

        protected override bool Execute(WithTime<TransferMetric> input)
        {
            var journey = input.EarliestArrivalJourney();
            NotNull(journey);
            NoLoops(journey, input);
            return true;
        }
    }
}