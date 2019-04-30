using System;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

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
            (TransitDb transitDb, string departureStopId, string arrivalStopId, DateTime
                departureTime, DateTime arrivalTime) input)
        {
            var latest = input.transitDb.Latest;
            var profile = new Profile<TransferMetric>(new InternalTransferGenerator(1),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory, TransferMetric.ProfileTransferCompare);

            // get departure and arrival stop ids.
            var reader = latest.StopsDb.GetReader();
            True(reader.MoveTo(input.departureStopId));
            var departure = reader.Id;
            True(reader.MoveTo(input.arrivalStopId));
            var arrival = reader.Id;

            var easJ = latest.SelectProfile(profile).SelectStops(departure, arrival)
                .SelectTimeFrame(input.departureTime, input.arrivalTime).EarliestArrivalJourney();

            var pcsJs =
                latest.SelectProfile(profile)
                    .SelectStops(departure, arrival)
                    .SelectTimeFrame(input.departureTime, input.arrivalTime)
                    .AllJourneys();
            var pcsJ = pcsJs.Last();

            Information(easJ.ToString(latest));
            Information(pcsJ.ToString(latest));

            // PCS could find a route which arrives at the same time, but departs later
            True(easJ.Root.DepartureTime() <= pcsJ.Root.DepartureTime());
            True(easJ.ArrivalTime() <= pcsJ.ArrivalTime());

            return true;
        }
    }
}