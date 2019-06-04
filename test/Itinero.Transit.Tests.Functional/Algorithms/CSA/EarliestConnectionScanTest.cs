using Itinero.Transit.Journey.Metric;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class EarliestConnectionScanTest : DefaultFunctionalTest<TransferMetric>
    {
        protected override bool Execute(WithTime<TransferMetric> input)
        {
            var journey = input.EarliestArrivalJourney();
            NotNull(journey);
            NoLoops(journey, input);
            return true;
        }
    }
}