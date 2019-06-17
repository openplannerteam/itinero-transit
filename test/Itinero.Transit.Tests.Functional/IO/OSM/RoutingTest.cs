using System;
using Itinero.Transit.Data;
using Itinero.Transit.IO.OSM;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Tests.Functional.Algorithms;

namespace Itinero.Transit.Tests.Functional.IO.OSM
{
    public class RoutingTest : FunctionalTest<object, object>
    {
        protected override object Execute(object input)
        {
            
            // Write your test here - it will always be executed on the server
            // If something is wrong, just throw an exception

            var tdb = TransitDb.ReadFrom(TestAllAlgorithms._nmbs, 0);
            
            var profile = new Profile<TransferMetric>(
                new InternalTransferGenerator(),
                new OsmTransferGenerator(),
                TransferMetric.Factory, TransferMetric.ProfileTransferCompare);

            tdb.SelectProfile(profile)
                .SelectStops(TestAllAlgorithms.Brugge, TestAllAlgorithms.Vielsalm)
                .SelectTimeFrame(DateTime.Now, DateTime.Now.AddHours(3))
                .EarliestArrivalJourney();

            return true;
        }
    }
}