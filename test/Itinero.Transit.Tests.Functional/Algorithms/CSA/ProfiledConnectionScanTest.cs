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
    public class ProfiledConnectionScanTest : FunctionalTest<IEnumerable<Journey<TransferStats>>, (ConnectionsDb
        connections, StopsDb
        stops,
        string departureStopId, string arrivalStopId, DateTime departureTime, DateTime arrivalTime)>
    {
        public static ProfiledConnectionScanTest Default => new ProfiledConnectionScanTest();

        protected override IEnumerable<Journey<TransferStats>> Execute((ConnectionsDb connections, StopsDb stops,
            string departureStopId, string arrivalStopId, DateTime departureTime, DateTime arrivalTime) input)
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


            var journeys = p.CalculateJourneys(
                departure, arrival, input.departureTime.ToUnixTime(), input.arrivalTime.ToUnixTime()
            );
            // verify result.
            Assert.NotNull(journeys);
            True(journeys.Any());

            foreach (var j in journeys)
            {
                Information(j.Pruned().ToString(input.stops));    
            }
            
            Information($"Found {journeys.Count()} profiles");
            
            return journeys;
        }
    }
}