using System;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class EarliestConnectionScanTest : DefaultFunctionalTest
    {
        public static EarliestConnectionScanTest Default => new EarliestConnectionScanTest();

        protected override bool Execute(
            (TransitDb transitDb, string departureStopId, string arrivalStopId, DateTime departureTime,
                DateTime arrivalTime) input)
        {
            var tbd = input.transitDb;
            var latest = tbd.Latest;
            var p = new Profile<TransferStats>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(latest),
                new TransferStats(), TransferStats.ProfileTransferCompare
                );

            var depTime = input.departureTime;

            // get departure and arrival stop ids.
            var reader = latest.StopsDb.GetReader();
            True(reader.MoveTo(input.departureStopId));
            var departure = reader.Id;
            True(reader.MoveTo(input.arrivalStopId));
            var arrival = reader.Id;

            // instantiate and run EAS.
            var eas = new EarliestConnectionScan<TransferStats>(
                latest,
                departure, arrival,
                depTime, depTime.AddHours(24), p);
            var journey = eas.CalculateJourney();

            if (journey == null)
            {
                Information($"Could not find a route from {input.departureStopId} to {input.arrivalStopId}");
            }

            // verify result.
            NotNull(journey);

            Information(journey.ToString(latest.StopsDb));

            return true;
        }
    }
}