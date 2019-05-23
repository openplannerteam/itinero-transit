using System.Linq;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    /// <summary>
    /// When running PCS (without pruning), the earliest route should equal the one calculated by EAS.
    /// If not  something is wrong
    /// </summary>
    public class EasPcsComparison : DefaultFunctionalTest<TransferMetric>
    {
        protected override bool Execute(WithTime<TransferMetric> input)
        {
            var easJ = input.EarliestArrivalJourney();
            input.ResetFilter();
            var pcsJs = input.AllJourneys();
            var pcsJ = pcsJs.Last();

            // PCS could find a route which arrives at the same time, but departs later
            True(easJ.Root.DepartureTime() <= pcsJ.Root.DepartureTime());
            True(easJ.ArrivalTime() <= pcsJ.ArrivalTime());
            NoLoops(easJ, input.StopsReader);
            foreach (var j in pcsJs)
            {
                NoLoops(j, input.StopsReader);
            }

            return true;
        }
    }
}