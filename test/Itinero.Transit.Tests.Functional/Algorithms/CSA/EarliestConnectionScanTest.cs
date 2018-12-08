using System;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Serilog;
using Xunit;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class EasTestBasic : FunctionalTest<string, (ConnectionsDb connections, StopsDb stops,
        string departureStopId, string arrivalStopId, DateTime departureTime)>
    {
        /// <summary>
        /// Gets the default test.
        /// </summary>
        public static EasTestBasic Default => new EasTestBasic();
        
        protected override string Execute((ConnectionsDb connections, StopsDb stops,
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

            // TODO: these conversions should not be needed.
            var departureId = (ulong) departure.localTileId * uint.MaxValue +
                         departure.localId;
            var arrivalId = (ulong) arrival.localTileId * uint.MaxValue +
                           arrival.localId;
            
            // instantiate and run EAS.
            var eas = new EarliestConnectionScan<TransferStats>(
                departureId, arrivalId,
                depTime, depTime.AddHours(24), p);
            var journey = eas.CalculateJourney();

            // verify result.
            Assert.NotNull(journey);
            Information(journey.ToString());

            return journey.ToString();
        }
    }
}