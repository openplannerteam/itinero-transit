using System.Linq;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.Tests.Functional.Utils;

// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class ProfiledConnectionScanTest :FunctionalTestWithInput<WithTime<TransferMetric>>
    {
        protected override void Execute()
        {
            Input.IsochroneFrom(); // Calculating the isochrone lines makes sure this is reused as filter - in some cases, testing goes from ~26 seconds to ~6

            var journeys = Input.AllJourneys();

            // verify result.
            NotNull(journeys);
            foreach (var journey in journeys)
            {
               AssertNoLoops(journey, Input);
            }

            True(journeys.Any());
        }
    }
}