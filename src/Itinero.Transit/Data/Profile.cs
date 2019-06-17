using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;

namespace Itinero.Transit.Data
{
    public class Profile<T>
        where T : IJourneyMetric<T>
    {
        
        
        public readonly T MetricFactory;
        public readonly ProfiledMetricComparator<T> ProfileComparator;
        
        public readonly IOtherModeGenerator InternalTransferGenerator;
        public readonly IOtherModeGenerator WalksGenerator;

        public Profile(IOtherModeGenerator internalTransferGenerator,
            IOtherModeGenerator walksGenerator,
            T metricFactory,
            ProfiledMetricComparator<T> profileComparator
        )
        {
            MetricFactory = metricFactory;
            ProfileComparator = profileComparator;
            InternalTransferGenerator = internalTransferGenerator;
            WalksGenerator = walksGenerator;
        }
    }
}