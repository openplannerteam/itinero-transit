using Itinero.Transit.Journey.Metric;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class IsochroneTest : DefaultFunctionalTest<TransferMetric>
    {

        protected override bool Execute(WithTime<TransferMetric> input)
        {
            var found = input.IsochroneFrom();
            True(found.Count > 10);

            found = input.IsochroneTo();
            True(found.Count > 10);
            
            return true;
        }


    }
}