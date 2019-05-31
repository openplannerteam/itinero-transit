using System;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
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
                new OsmTransferGenerator(null), // This will probably change
                TransferMetric.Factory, TransferMetric.ProfileTransferCompare
                );
            
            tdb.SelectProfile(profile)
                .SelectStops(TestAllAlgorithms.Brugge, TestAllAlgorithms.Vielsalm);
            
            throw new NotImplementedException("IMPLEMENT ME BEN");
            
            
        }
    }
}