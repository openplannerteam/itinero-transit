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
        public readonly MetricComparator<T> ProfileComparator;
        
        public readonly IOtherModeGenerator InternalTransferGenerator;
        public readonly IOtherModeGenerator WalksGenerator;

        public readonly IConnectionFilter ConnectionFilter;
        public readonly IJourneyFilter<T> JourneyFilter;

        public Profile(IOtherModeGenerator internalTransferGenerator,
            IOtherModeGenerator walksGenerator,
            T metricFactory,
            MetricComparator<T> profileComparator, IConnectionFilter connectionFilter = null, IJourneyFilter<T> journeyFilter = null)
        {
            MetricFactory = metricFactory;
            ProfileComparator = profileComparator;
            ConnectionFilter = connectionFilter;
            JourneyFilter = journeyFilter;
            InternalTransferGenerator = internalTransferGenerator;
            WalksGenerator = walksGenerator;
        }
    }
}