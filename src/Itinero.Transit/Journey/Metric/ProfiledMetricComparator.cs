namespace Itinero.Transit.Journey.Metric
{
    /// <inheritdoc />
    /// <summary>
    /// A special subtype of MetricComparators.
    /// MetricComparators should focus on comparing time ranges
    /// (thus A only dominates B if a.startTime > b.startTime && a.endTime &lt; b.endTime).
    /// 
    /// Note that this is far away from comparing the total travel times!
    /// For example, a journey in the morning taking one hour and one in the afternoon taking 1h1m,
    /// should keep both as being non-dominated.
    /// 
    /// This is used in the profileConnectionScan.
    ///
    /// Note that this class does not implement extra methods. It acts purely as a marker
    /// </summary>
    public abstract class ProfiledMetricComparator<T> : MetricComparator<T>
        where T : IJourneyMetric<T>
    {
    }
}