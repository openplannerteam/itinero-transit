using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Filter;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;

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
            IJourneyMetric<T> profileMetricFactory,
            ProfiledMetricComparator<T> profileProfileComparator,
            IOtherModeGenerator profileInternalTransferGenerator,
            IOtherModeGenerator profileWalksGenerator,
            List<(LocationId, Journey<T>)> from, List<(LocationId, Journey<T>)> to)
        {
            StopsReader = stopsReader;
            ConnectionsEnumerator = connectionsEnumerator;
            EarliestDeparture = start;
            LastArrival = end;

            DepartureStop = from;
            TargetStop = to;

            MetricFactory = (T) profileMetricFactory;
            Comparator = profileProfileComparator;
            TransferPolicy = profileInternalTransferGenerator;
            WalkPolicy = profileWalksGenerator;
        }


        public IStopsReader StopsReader { get; }
        public IConnectionEnumerator ConnectionsEnumerator { get; }

        public DateTime EarliestDeparture { get; }
        public DateTime LastArrival { get; }

        public List<(LocationId, Journey<T>)> DepartureStop { get; }
        public List<(LocationId, Journey<T>)> TargetStop { get; }
        public T MetricFactory { get; }
        public ProfiledMetricComparator<T> Comparator { get; }
        public IOtherModeGenerator TransferPolicy { get; }
        public IOtherModeGenerator WalkPolicy { get; }
        public IConnectionFilter Filter { get; set; }
    }
}