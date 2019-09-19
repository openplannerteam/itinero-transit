using System;
using System.Collections.Generic;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
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
        private readonly IConnectionEnumerator _clock;

        private readonly StopId _departureStop;

        private HashSet<ProfiledParetoFrontier<T>> _alreadyCleaned;
        private ulong _alreadyCleanedScanTime = uint.MaxValue;

        /// <summary>
        /// Create a new SimpleMetric
        /// </summary>
        /// <param name="clock">The 'clock' is a IConnectionReader, IConnectionEnumerator or something _stateful_. The departure time should regularly update to reflect departure time PCS is scanning </param>
        /// <param name="departureStop">A normal ID where to teleport too</param>
        public SimpleMetricGuesser(IConnectionEnumerator clock, StopId departureStop)
        {
            _clock = clock;
            // It doesn't matter a whole lot what the exact destination stop is
            _departureStop = departureStop;
        }

        public T LeastTheoreticalConnection(Journey<T> intermediate, out ulong departureTime)
        {
            
            var teleportation = new Connection(ConnectionId.Invalid,
                "https://en.wikipedia.org/wiki/Teleportation",
                _departureStop, intermediate.Location,
                _clock.CurrentDateTime, // The current connection scan is here, future departures will only be sooner
                0, // Traveltime is 0 - we are talking about Teleportation after all!
                0,
                0,
                0,
                intermediate.TripId
            );
            var j = intermediate.ChainBackward(teleportation);
            departureTime = j.Time;
            return j.Metric;
            /*
            departureTime = _clock.CurrentDateTime;
            return intermediate.Metric.Add(intermediate, _departureStop, _clock.CurrentDateTime, 
                intermediate.TripId,
                true); // The 'special bit' is true, as this will make sure no extra vehicle is added
                */
        }

        public bool ShouldBeChecked(ProfiledParetoFrontier<T> frontier)
        {
            var curScanTime = _clock.CurrentDateTime;
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