using System;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Xunit;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class LatestConnectionScanTest : FunctionalTest<Journey<TransferStats>, (ConnectionsDb connections, StopsDb
        stops,
        string departureStopId, string arrivalStopId, DateTime departureTime)>
    {
        public static LatestConnectionScanTest Default => new LatestConnectionScanTest();

        protected override Journey<TransferStats> Execute((ConnectionsDb connections, StopsDb stops,
            string departureStopId, string arrivalStopId, DateTime departureTime) input)
        {
            var p = new Profile<TransferStats>(
                input.connections, input.stops,
                new InternalTransferGenerator(), new TransferStats(), TransferStats.ProfileTransferCompare);


            // get departure and arrival stop ids.
            var reader = input.stops.GetReader();
            True(reader.MoveTo(input.departureStopId));
            var departure = reader.Id;
            True(reader.MoveTo(input.arrivalStopId));
            var arrival = reader.Id;

            // instantiate and run EAS.
            var las = new LatestConnectionScan<TransferStats>(
                departure, arrival,
                input.departureTime, input.departureTime.AddHours(24), p);
            var journey = las.CalculateJourney();

            // verify result.
            Assert.NotNull(journey);

            return journey;
        }
    }
}