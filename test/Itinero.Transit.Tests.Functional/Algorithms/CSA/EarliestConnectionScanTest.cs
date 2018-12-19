using System;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Xunit;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class EarliestConnectionScanTest : DefaultFunctionalTest
    {
        public static EarliestConnectionScanTest Default => new EarliestConnectionScanTest();

        protected override bool Execute(
            (ConnectionsDb connections, StopsDb stops, string departureStopId, string arrivalStopId, DateTime departureTime,
                DateTime arrivalTime) input)
        {
            var p = new Profile<TransferStats>(
                input.connections, input.stops,
                new InternalTransferGenerator(), 
                new BirdsEyeInterWalkTransferGenerator(input.stops.GetReader()), 
                new TransferStats(), TransferStats.ProfileTransferCompare);

            var depTime =input.departureTime;

            // get departure and arrival stop ids.
            var reader = input.stops.GetReader();
            True(reader.MoveTo(input.departureStopId));
            var departure = reader.Id;
            True(reader.MoveTo(input.arrivalStopId));
            var arrival = reader.Id;

            // instantiate and run EAS.
            var eas = new EarliestConnectionScan<TransferStats>(
                departure, arrival,
                depTime, depTime.AddHours(24), p);
            var journey = eas.CalculateJourney();

            if (journey == null)
            {
                Information($"Could not find a route from {input.departureStopId} to {input.arrivalStopId}");
            }
            
            // verify result.
            Assert.NotNull(journey);

            Information(journey.Pruned().ToString(input.stops));
            
            return true;
        }

      
    }
}