using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Xunit;

// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    public class ProfiledConnectionScanTest :
        DefaultFunctionalTest
    {
        public static ProfiledConnectionScanTest Default => new ProfiledConnectionScanTest();

        protected override bool Execute((ConnectionsDb connections, StopsDb stops,
            string departureStopId, string arrivalStopId, DateTime departureTime, DateTime arrivalTime) input)
        {
            var dbs = new Databases(
                input.connections, input.stops,
                new InternalTransferGenerator(),
                new BirdsEyeInterWalkTransferGenerator(input.stops.GetReader()));
            var p = new Profile<TransferStats>(dbs,
                new TransferStats(), TransferStats.ProfileTransferCompare);


            // get departure and arrival stop ids.
            var reader = input.stops.GetReader();
            True(reader.MoveTo(input.departureStopId));
            var departure = reader.Id;
            True(reader.MoveTo(input.arrivalStopId));
            var arrival = reader.Id;


            var journeys = p.CalculateJourneys(
                departure, arrival, input.departureTime.ToUnixTime(), input.arrivalTime.ToUnixTime()
            );
            // verify result.
            Assert.NotNull(journeys);
            True(journeys.Any());

            Information($"Found {journeys.Count()} profiles");

            return true;
        }
    }
}