using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Reminiscence.Collections;

// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class ProfiledConnectionScanTest :
        DefaultFunctionalTest<TransferMetric>
    {
        protected override bool Execute(WithTime<TransferMetric> input)
        {
            input.IsochroneFrom(); // Calculating the isochrone lines makes sure this is reused as filter - in some cases, testing goes from ~26 seconds to ~6

            var pcs = new ProfiledConnectionScan<TransferMetric>(input.GetScanSettings());
            var journeys = pcs.CalculateJourneys();

            // verify result.
            NotNull(journeys);
            var withLoop = new List<Journey<TransferMetric>>();
            foreach (var journey in journeys)
            {
                if (ContainsLoop(journey))
                {
                    withLoop.Add(journey);
                }
            }

            True(journeys.Any());

            Information($"Found {journeys.Count} profiles");

            return true;
        }
    }
}