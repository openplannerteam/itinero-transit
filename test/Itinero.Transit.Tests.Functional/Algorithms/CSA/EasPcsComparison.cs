using System.Linq;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.Tests.Functional.Utils;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    /// <summary>
    /// When running PCS (without pruning), the earliest route should equal the one calculated by EAS.
    /// If not  something is wrong
    /// </summary>
    public class EasPcsComparison : FunctionalTestWithInput<WithTime<TransferMetric>>
    {
        protected override void Execute()
        {
            var easJ = Input.EarliestArrivalJourney(
                (tuple => Input.End));
            
            var pcsJs = Input.AllJourneys();
            var pcsJ = pcsJs.Last();

            // PCS could find a route which arrives at the same time, but departs later
            True(easJ.Root.DepartureTime() <= pcsJ.Root.DepartureTime());
            True(easJ.ArrivalTime() <= pcsJ.ArrivalTime());
            AssertNoLoops(easJ, Input);
            foreach (var j in pcsJs)
            {
                AssertNoLoops(j, Input);
            }

        }
    }
}