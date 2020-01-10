using Itinero.Transit.Journey.Metric;
using Itinero.Transit.Tests.Functional.Utils;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class EarliestConnectionScanTest : FunctionalTestWithInput<WithTime<TransferMetric>>
    {
        public override string Name => "EAS";

        protected override void Execute()
        {
            var journey = Input.CalculateEarliestArrivalJourney();
            NotNull(journey);
            AssertNoLoops(journey, Input);
        }
    }
}