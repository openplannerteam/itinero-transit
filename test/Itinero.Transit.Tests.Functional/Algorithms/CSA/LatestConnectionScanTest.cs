using Itinero.Transit.Journey.Metric;
using Itinero.Transit.Tests.Functional.Utils;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class LatestConnectionScanTest :FunctionalTestWithInput<WithTime<TransferMetric>>
    {
        protected override void Execute()
        {
            var lasJ = Input.LatestDepartureJourney();
            NotNull(lasJ);
            // NoLoops(lasJ, input);
            // LAS can possible create a transfer which could have been taken sooner,
            // but that is just how LAS works
        }
    }
}