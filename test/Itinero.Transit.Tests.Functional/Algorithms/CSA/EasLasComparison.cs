using Itinero.Transit.Journey.Metric;
using Itinero.Transit.Tests.Functional.Utils;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    /// <summary>
    /// When running PCS (without pruning), the earliest route should equal the one calculated by EAS.
    /// If not  something is wrong
    /// </summary>
    public class EasLasComparison : FunctionalTestWithInput<WithTime<TransferMetric>>
    {
        protected override void Execute( )
        {
            var easJ =
                Input.EarliestArrivalJourney();

            NotNull(easJ);
            AssertNoLoops(easJ, Input);

            Input.ResetFilter();

            var lasJ =
                Input
                    .DifferentTimes(easJ.Root.DepartureTime().FromUnixTime(),
                       easJ.ArrivalTime().FromUnixTime())
                    .LatestDepartureJourney();


            var stop = Input.StopsReader;
            stop.MoveTo(Input.From[0].Item1);
            var id0 = stop.GlobalId;
            stop.MoveTo(Input.To[0].Item1);
            var id1 = stop.GlobalId;
            stop.Attributes.TryGetValue("name", out var name);
            NotNull(lasJ,
                $"No latest journey found for {id0} {Input.Start:s} --> {id1}({name}). However, the earliest arrival journey has been found:" +
                $"\n{easJ.ToString(Input)}");
            AssertNoLoops(lasJ, Input);

            // Eas is bound by the first departing train, while las is not
            True(easJ.Root.DepartureTime() <= lasJ.Root.DepartureTime());
            True(easJ.ArrivalTime() == lasJ.ArrivalTime());
        }
    }
}