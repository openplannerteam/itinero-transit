using Itinero.Transit.Algorithms.Filter;
using Itinero.Transit.Journey;
using Itinero.Transit.Logging;
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
            if (InternalTransferGenerator.Range() != 0)
            {
                Log.Warning("The profile has an internal transfer generator with range != 0. This is highly suspicious. Perhaps you swapped the InternalTransferGenerator and WalkGenerator-arguments?");
            }
            if (WalksGenerator.Range() < 1)
            {
                Log.Warning("The profile has an walkGenerator with range 0 (or very small). This is highly suspicious in real-life deployements. (Ignore this if you do not want intermodal transfers before, during or after the journeys)");
            }
        }
    }
}