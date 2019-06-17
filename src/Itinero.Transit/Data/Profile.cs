using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Filter;
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
        public IOtherModeGenerator FirstMileWalksGenerator, LastMileWalksGenerator;

        public readonly IJourneyFilter<T> JourneyFilter;

        public Profile(IOtherModeGenerator internalTransferGenerator,
            IOtherModeGenerator walksGenerator,
            T metricFactory,
            ProfiledMetricComparator<T> profileComparator, IJourneyFilter<T> journeyFilter = null)
        {
            MetricFactory = metricFactory;
            ProfileComparator = profileComparator;
            JourneyFilter = journeyFilter;
            InternalTransferGenerator = internalTransferGenerator;
            WalksGenerator = walksGenerator;
            FirstMileWalksGenerator = walksGenerator;
            LastMileWalksGenerator = walksGenerator;
        }
        
    }
}