using System;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class ProfiledConnectionScanTest :
        DefaultFunctionalTest
    {
        public static ProfiledConnectionScanTest Default => new ProfiledConnectionScanTest();

        protected override bool Execute(
            (TransitDb transitDb, string departureStopId, string arrivalStopId, DateTime
                departureTime, DateTime arrivalTime) input)
        {
            var tbd = input.transitDb;
            var latest = tbd.Latest;
            var p = new Profile<TransferStats>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(latest),
                new TransferStats(), TransferStats.ProfileTransferCompare);


            // get departure and arrival stop ids.
            var reader = latest.StopsDb.GetReader();
            True(reader.MoveTo(input.departureStopId));
            var departure = reader.Id;
            True(reader.MoveTo(input.arrivalStopId));
            var arrival = reader.Id;

            var journeys = tbd.CalculateJourneys(p,
                departure, arrival, input.departureTime.ToUnixTime(), input.arrivalTime.ToUnixTime()
            );
            // verify result.
            NotNull(journeys);
            True(journeys.Any());

            Information($"Found {journeys.Count()} profiles");

            return true;
        }
    }
}