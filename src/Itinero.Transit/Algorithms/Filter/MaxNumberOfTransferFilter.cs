using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;

namespace Itinero.Transit.Algorithms.Filter
{
    public class MaxNumberOfTransferFilter : IJourneyFilter<TransferMetric>
    {
        private readonly uint _maxNumberOfTransfers;

        public MaxNumberOfTransferFilter(uint maxNumberOfTransfers = 3)
        {
            _maxNumberOfTransfers = maxNumberOfTransfers;
        }

        public bool CanBeTaken(Journey<TransferMetric> journey)
        {
            return journey.Metric.NumberOfTransfers <= _maxNumberOfTransfers;
        }

        public bool CanBeTakenBackwards(Journey<TransferMetric> journey)
        {
            return journey.Metric.NumberOfTransfers <= _maxNumberOfTransfers;
        }
    }
}