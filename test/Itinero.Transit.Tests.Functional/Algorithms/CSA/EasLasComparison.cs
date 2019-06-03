using Itinero.Transit.Journeys;
using Reminiscence.Arrays;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    /// <summary>
    /// When running PCS (without pruning), the earliest route should equal the one calculated by EAS.
    /// If not  something is wrong
    /// </summary>
    public class EasLasComparison : DefaultFunctionalTest<TransferMetric>
    {
        protected override bool Execute(WithTime<TransferMetric> input)
        {
            var easJ =
                input.EarliestArrivalJourney();

            NotNull(easJ);
            NoLoops(easJ, input);

            input.ResetFilter();
            
            var lasJ =
                input
                    .DifferentTimes((easJ.Root.DepartureTime()-600).FromUnixTime(),
                        (easJ.ArrivalTime()+600).FromUnixTime())
                    .LatestDepartureJourney();


            var stop = input.StopsReader;
            stop.MoveTo(input.From[0].Item1);
            var id0 = stop.GlobalId;
            stop.MoveTo(input.To[0].Item1);
            var id1 = stop.GlobalId;
            stop.Attributes.TryGetValue("name", out var name);
            NotNull(lasJ,
                $"No latest journey found for {id0} {input.Start:s} --> {id1}({name}). However, the earliest arrival journey has been found:" +
                $"\n{easJ.ToString(input)}");
            NoLoops(lasJ, input);

            // Eas is bound by the first departing train, while las is not
            True(easJ.Root.DepartureTime() <= lasJ.Root.DepartureTime());
            True(easJ.ArrivalTime() == lasJ.ArrivalTime());


            return true;
        }
    }
}