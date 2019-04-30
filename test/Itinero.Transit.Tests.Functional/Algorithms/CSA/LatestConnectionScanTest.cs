using System;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class LatestConnectionScanTest :
        DefaultFunctionalTest
    {
        public static LatestConnectionScanTest Default => new LatestConnectionScanTest();

        protected override bool Execute(
            (TransitDb transitDb, string departureStopId, string arrivalStopId, DateTime
                departureTime, DateTime arrivalTime) input)
        {
            var latest = input.transitDb.Latest;
            var p = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory, TransferMetric.ProfileTransferCompare);

            // get departure and arrival stop ids.
            var reader = latest.StopsDb.GetReader();
            True(reader.MoveTo(input.departureStopId));
            var departure = reader.Id;
            True(reader.MoveTo(input.arrivalStopId));
            var arrival = reader.Id;

            var journey = latest.SelectProfile(p)
                .SelectStops(departure, arrival)
                .SelectTimeFrame(input.departureTime, input.arrivalTime);

            // verify result.
            NotNull(journey);

            return true;
        }
    }
}