using System;
using System.Linq;
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
            var p = new Profile<TransferMetric>(new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory, TransferMetric.ProfileTransferCompare);


            var journeys = latest.SelectProfile(p)
                .SelectStops(input.departureStopId, input.arrivalStopId)
                .SelectTimeFrame(input.departureTime, input.arrivalTime)
                .AllJourneys();
            // verify result.
            NotNull(journeys);
            True(journeys.Any());

            Information($"Found {journeys.Count()} profiles");

            return true;
        }
    }
}