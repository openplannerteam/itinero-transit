using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey;

namespace Itinero.Transit.Algorithms.Filter
{
    /// <summary>
    /// The simple metric guesser 'teleports' the traveller to the destination at the current
    /// clock time.
    ///
    /// This metricGuesser is built for BACKWARD journeys
    ///
    /// Extremely simple thus!
    ///
    /// It keeps track of the PCS-scan-clock and what frontiers have been cleaned already this tick.
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SimpleMetricGuesser<T> : IMetricGuesser<T> where T : IJourneyMetric<T>
    {

        private readonly StopId _departureStop;

        private HashSet<ProfiledParetoFrontier<T>> _alreadyCleaned;
        private ulong _alreadyCleanedScanTime = uint.MaxValue;

        /// <summary>
        /// Create a new SimpleMetric
        /// </summary>
        /// <param name="departureStop">A normal ID where to teleport too</param>
        public SimpleMetricGuesser(StopId departureStop)
        {
            _departureStop = departureStop;
        }

        public SimpleMetricGuesser(IEnumerable<StopId> departureStops)
            :this(departureStops.First())
            // It doesn't matter what the exact comparison stop is - it is just used to 'teleport' there; afterwards we see if the collection is still in the optimal set. This is done by PCS
        {
        }

        public T LeastTheoreticalConnection(Journey<T> intermediate, ulong currentTime, out ulong departureTime)
        {
            departureTime = currentTime;
            var m = intermediate.Metric.Add(intermediate, _departureStop, currentTime, 
                intermediate.TripId,
                true); // The 'special bit' is true, as this will make sure no extra vehicle is added
            return m;
        }

        public bool ShouldBeChecked(ProfiledParetoFrontier<T> frontier, ulong currentTime)
        {
            // ReSharper disable once InvertIf
            if (currentTime != _alreadyCleanedScanTime)
            {
                _alreadyCleaned = new HashSet<ProfiledParetoFrontier<T>>();
                _alreadyCleanedScanTime = currentTime;
            }

            return _alreadyCleaned.Add(frontier);
        }
    }
}