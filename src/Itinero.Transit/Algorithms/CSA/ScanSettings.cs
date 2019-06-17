using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Filter;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Reminiscence.Arrays;

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
            List<(LocationId, Journey<T>)> from, List<(LocationId, Journey<T>)> to)
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

        public List<(LocationId, Journey<T>)> DepartureStop { get; }
        public List<(LocationId, Journey<T>)> TargetStop { get; }
        public Profile<T> Profile { get; }
        public IConnectionFilter Filter { get; set; }
    }
}