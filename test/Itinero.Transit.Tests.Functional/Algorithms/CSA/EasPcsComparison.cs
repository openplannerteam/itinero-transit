using System;
using System.Linq;
using Itinero.IO.LC;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Xunit;

namespace Itinero.Transit.Tests.Functional.Algorithms.CSA
{
    /// <summary>
    /// When running PCS (without pruning), the earliest route should equal the one calculated by EAS.
    /// If not  something is wrong
    /// </summary>
    public class EasPcsComparison : DefaultFunctionalTest
    {
        public static EasPcsComparison Default = new EasPcsComparison();

        protected override bool Execute(
            (ConnectionsDb connections, StopsDb stops, string departureStopId, string arrivalStopId, DateTime
                departureTime, DateTime arrivalTime) input)
        {
            var profile = new Profile<TransferStats>(
                input.connections, input.stops,
                new InternalTransferGenerator(1),
                new BirdsEyeInterWalkTransferGenerator(input.stops.GetReader()),
                TransferStats.Factory, TransferStats.ProfileTransferCompare);

            // get departure and arrival stop ids.
            var reader = input.stops.GetReader();
            True(reader.MoveTo(input.departureStopId));
            var departure = reader.Id;
            True(reader.MoveTo(input.arrivalStopId));
            var arrival = reader.Id;

            var eas = new EarliestConnectionScan<TransferStats>(
                departure, arrival, input.departureTime, input.arrivalTime, profile);

            var easJ = eas.CalculateJourney();
            var pcs = new ProfiledConnectionScan<TransferStats>(
                departure, arrival, input.departureTime, input.arrivalTime, profile);

            var pcsJs = pcs.CalculateJourneys();
            var pcsJ = pcsJs.Last();

            Information(easJ.ToString(input.stops));
            Information(pcsJ.ToString(input.stops));

            // PCS could find a route which arrives at the same time, but departs later
            Assert.True(easJ.Root.DepartureTime() <= pcsJ.Root.DepartureTime());
            Assert.True(easJ.ArrivalTime() <= pcsJ.ArrivalTime());

            return true;
        }
    }
}