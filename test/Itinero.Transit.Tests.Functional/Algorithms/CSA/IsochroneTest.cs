using Itinero.Transit.Journey.Metric;
using Itinero.Transit.Tests.Functional.Utils;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class IsochroneTest :FunctionalTestWithInput<WithTime<TransferMetric>>
    {
        protected override void Execute()
        {
            var found = Input.CalculateIsochroneFrom();
            True(found.Count > 10);

            found = Input.CalculateIsochroneTo();
            True(found.Count > 10);
        }


    }
}