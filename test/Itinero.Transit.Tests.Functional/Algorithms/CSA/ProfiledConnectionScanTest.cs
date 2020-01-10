using System.Linq;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.Tests.Functional.Utils;

// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class ProfiledConnectionScanTest :FunctionalTestWithInput<WithTime<TransferMetric>>
    {      public override string Name => "PCS";

        protected override void Execute()
        {
            Input.CalculateIsochroneFrom(); // Calculating the isochrone lines makes sure this is reused as filter - in some cases, testing goes from ~26 seconds to ~6

            var journeys = Input.CalculateAllJourneys();

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