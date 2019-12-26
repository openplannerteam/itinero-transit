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
            IStopsDb stopsReader,
            IConnectionsDb connections,
            DateTime start,
            DateTime end,
            Profile<T> profile,
            List<Stop> from, List<Stop> to)
        {
            Stops = stopsReader;
            Connections = connections;
            EarliestDeparture = start;
            LastArrival = end;

            DepartureStop = from;
            TargetStop = to;

            Profile = profile;
        }


        public IStopsDb Stops { get; }
        public IConnectionsDb Connections { get; }

        public DateTime EarliestDeparture { get; }
        public DateTime LastArrival { get; }

        public List<Stop> DepartureStop { get; }
        public List<Stop> TargetStop { get; }
        public Profile<T> Profile { get; }
        public IsochroneFilter<T> Filter { get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public IMetricGuesser<T> MetricGuesser { get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public Journey<T> ExampleJourney { get; set; }
    }
}