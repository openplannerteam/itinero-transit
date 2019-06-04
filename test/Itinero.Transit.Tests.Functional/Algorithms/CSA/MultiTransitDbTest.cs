using Itinero.Transit.Journey.Metric;

// ReSharper disable UnusedMember.Local

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class MultiTransitDbTest : DefaultFunctionalTest<TransferMetric>
    {
        protected override bool Execute(WithTime<TransferMetric> input)
        {
            input.IsochroneFrom();

            var journeys = input.AllJourneys();
            NotNull(journeys);
            True(journeys.Count > 0);
            foreach (var journey in journeys)
            {
                NoLoops(journey, input);
            }

            return true;
        }
    }
}