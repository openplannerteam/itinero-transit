using System.Linq;
using Itinero.Transit.Journeys;

// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class ProfiledConnectionScanTest :
        DefaultFunctionalTest<TransferMetric>
    {
        protected override bool Execute(WithTime<TransferMetric> input)
        {
            input.IsochroneFrom(); // Calculating the isochrone lines makes sure this is reused as filter - in some cases, testing goes from ~26 seconds to ~6
            var journeys = input.AllJourneys();
            // verify result.
            NotNull(journeys);
            True(journeys.Any());

            Information($"Found {journeys.Count} profiles");

            return true;
        }
    }
}