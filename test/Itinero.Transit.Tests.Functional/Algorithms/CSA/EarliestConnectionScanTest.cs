using System;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Xunit;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class EarliestConnectionScanTest : FunctionalTest<Journey<TransferStats>, (ConnectionsDb connections, StopsDb stops,
        string departureStopId, string arrivalStopId, DateTime departureTime)>
    {
        /// <summary>
        /// Gets the default test.
        /// </summary>
        public static EarliestConnectionScanTest Default => new EarliestConnectionScanTest();
        
        protected override Journey<TransferStats> Execute((ConnectionsDb connections, StopsDb stops,
            string departureStopId, string arrivalStopId, DateTime departureTime) input)
        {
            var p = new Profile<TransferStats>(
                input.connections, input.stops,
                new NoWalksGenerator(), new TransferStats());

            var depTime = DateTime.Now.Date.AddMinutes(10 * 60 + 25);

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

            // verify result.
            Assert.NotNull(journey);

            return journey;
        }
    }
}