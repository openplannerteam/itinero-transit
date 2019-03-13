using System;
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
    public class EasLasComparison : DefaultFunctionalTest
    {
        public static readonly EasLasComparison Default = new EasLasComparison();

        protected override bool Execute(
            (TransitDb transitDb, string departureStopId, string arrivalStopId, DateTime
                departureTime, DateTime arrivalTime) input)
        {
            var latest = input.transitDb.Latest;
            var profile = new Profile<TransferStats>(new InternalTransferGenerator(1),
                new CrowsFlightTransferGenerator(),
                TransferStats.Factory, TransferStats.ProfileTransferCompare);

            // get departure and arrival stop ids.
            var reader = latest.StopsDb.GetReader();
            True(reader.MoveTo(input.departureStopId));
            var departure = reader.Id;
            True(reader.MoveTo(input.arrivalStopId));
            var arrival = reader.Id;

            var eas = new EarliestConnectionScan<TransferStats>(
                new ScanSettings<TransferStats>(
                    latest,
                    departure, arrival,
                    input.departureTime,
                    input.arrivalTime,
                    profile));

            var easJ = eas.CalculateJourney();

            var las = new LatestConnectionScan<TransferStats>(new ScanSettings<TransferStats>(latest,
                departure, arrival, input.departureTime,
                easJ.ArrivalTime().FromUnixTime(), profile));

            var lasJ = las.CalculateJourney();

            Information(easJ.Pruned().ToString(latest.StopsDb));
            Information(lasJ.Pruned().ToString(latest.StopsDb));

            // Eas is bound by the first departing train, while las is not
            True(easJ.Root.DepartureTime() <= lasJ.Root.DepartureTime());
            True(easJ.ArrivalTime() >= lasJ.ArrivalTime());

            return true;
        }
    }
}