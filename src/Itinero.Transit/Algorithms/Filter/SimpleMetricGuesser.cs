using System.Collections.Generic;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;

namespace Itinero.Transit.Journey.Filter
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
    internal class SimpleMetricGuesser<T> : IMetricGuesser<T> where T : IJourneyMetric<T>
    {
        private readonly IConnection _clock;

        private readonly LocationId _departureStop;

        private HashSet<ProfiledParetoFrontier<T>> _alreadyCleaned;
        private ulong _alreadyCleanedScanTime = uint.MaxValue;

        /// <summary>
        /// Create a new SimpleMetric
        /// </summary>
        /// <param name="clock">The 'clock' is a IConnectionReader, IConnectionEnumerator or something _stateful_. The departure time should regularly update to reflect departure time PCS is scanning </param>
        /// <param name="departureStop">A normal ID where to teleport too</param>
        public SimpleMetricGuesser(IConnection clock, LocationId departureStop)
        {
            _clock = clock;
            // It doesn't matter a whole lot what the exact destination stop is
            _departureStop = departureStop;
        }

        public IConnection LeastTheoreticalConnection(Journey<T> intermediate)
        {
            var teleportation = new SimpleConnection(uint.MaxValue, "https://en.wikipedia.org/wiki/Teleportation",
                _departureStop, intermediate.Location,
                _clock.DepartureTime, // The current connection scan is here, future departures will only be sooner
                0, // Traveltime is 0 - we are talking about Teleportation after all!
                0,
                0,
                0,
                intermediate.TripId
            );

            return teleportation;
        }

        public bool ShouldBeChecked(ProfiledParetoFrontier<T> frontier)
        {
            var curScanTime = _clock.DepartureTime;
            // ReSharper disable once InvertIf
            if (curScanTime != _alreadyCleanedScanTime)
            {
                _alreadyCleaned = new HashSet<ProfiledParetoFrontier<T>>();
                _alreadyCleanedScanTime = curScanTime;
            }

            return _alreadyCleaned.Add(frontier);
        }
    }
}