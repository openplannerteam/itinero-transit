using System;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Data
{
    public class Profile<T>
        where T : IJourneyMetric<T>
    {
        
        
        public readonly T MetricFactory;
        public readonly ProfiledMetricComparator<T> ProfileComparator;
        
        public readonly IOtherModeGenerator InternalTransferGenerator;
        public readonly IOtherModeGenerator WalksGenerator;

        /// <summary>
        /// When given an earliest departure time, we want to figure out in what timespan we should calculate profiled journeys.
        /// For this, we first calculate an EAS route and then, based on the duration of it, calculate a last allowed arrival time
        /// </summary>
        public Func<TimeSpan, TimeSpan> LookAhead = ts => TimeSpan.FromSeconds(Math.Min(ts.TotalSeconds * 2, 24*60*60));
        
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