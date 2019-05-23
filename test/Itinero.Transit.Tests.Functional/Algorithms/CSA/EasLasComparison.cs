using Itinero.Transit.Journeys;

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
            NoLoops(easJ, input.StopsReader);

            var stop = input.StopsReader;
            stop.MoveTo(input.From[0].Item1);
            var id0 = stop.GlobalId;
            stop.MoveTo(input.To[0].Item1);
            var id1 = stop.GlobalId;

            var lasJ =
                input
                    .DifferentTimes(easJ.Root.DepartureTime().FromUnixTime(), easJ.ArrivalTime().FromUnixTime())
                    .LatestDepartureJourney();


            NotNull(lasJ,
                $"No latest journey found for {id0} {input.Start:s} --> {id1}, {easJ.ArrivalTime().FromUnixTime():s},\n{easJ}");
            NoLoops(lasJ, input.StopsReader);

            // Eas is bound by the first departing train, while las is not
            True(easJ.Root.DepartureTime() <= lasJ.Root.DepartureTime());
            True(easJ.ArrivalTime() == lasJ.ArrivalTime());


            return true;
        }
    }
}