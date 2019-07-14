using System;
using System.Collections.Generic;
using Itinero.Transit.Algorithms.Filter;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey;

namespace Itinero.Transit.Algorithms.CSA
{
    /// <summary>
    /// Scansettings is a small object keeping track of all common parameters to run a scan
    /// </summary>
    internal class ScanSettings<T> where T : IJourneyMetric<T>
    {
        public ScanSettings(
            IStopsReader stopsReader,
            IConnectionEnumerator connectionsEnumerator,
            DateTime start,
            DateTime end,
            Profile<T> profile,
            List<(StopId, Journey<T>)> from, List<(StopId, Journey<T>)> to)
        {
            StopsReader = stopsReader;
            ConnectionsEnumerator = connectionsEnumerator;
            EarliestDeparture = start;
            LastArrival = end;

            DepartureStop = from;
            TargetStop = to;

            Profile = profile;
        }


        public IStopsReader StopsReader { get; }
        public IConnectionEnumerator ConnectionsEnumerator { get; }

        public DateTime EarliestDeparture { get; }
        public DateTime LastArrival { get; }

        public List<(StopId, Journey<T>)> DepartureStop { get; }
        public List<(StopId, Journey<T>)> TargetStop { get; }
        public Profile<T> Profile { get; }
        public IsochroneFilter<T> Filter { get; set; }

        public IMetricGuesser<T> MetricGuesser { get; set; }

        public Journey<T> ExampleJourney { get; set; }
    }
}