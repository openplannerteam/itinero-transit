using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Algorithms.CSA
{
    /// <summary>
    /// Scansettings is a small object keeping track of all common parameters to run a scan
    /// </summary>
    internal class ScanSettings<T> where T : IJourneyMetric<T>
    {
        internal IConnectionFilter _filter;

        /// <summary>
        /// The connections that can be used, orderd by departure time
        /// </summary>
        public IConnectionEnumerator Connections { get; set; }

        internal IStopsReader StopsDbReader { get; set; }

        /// <summary>
        /// The earliest time the traveller wants to depart.
        /// In the case of LAS, this is a timeout if no route is found (or to calculate isochrone lines)
        /// </summary>
        internal DateTime EarliestDeparture { get; set; }

        /// <summary>
        /// The latest time the traveller wants to arrive.
        /// In case of EAS, this acts as a timeout if no route is found (or to calculate isochrone lines)
        /// </summary>
        internal DateTime LastArrival { get; set; }

        /// <summary>
        /// A list of possible departure locations with possible departure journeys.
        /// Journeys should be in a forward order (genesis, then take...)
        /// </summary>
        internal List<(LocationId, Journey<T>)> DepartureStop { get; set; }

        /// <summary>
        /// A list of possible arrival locations with possible arrival journeys
        /// Journeys should be in a backward order (..., then take to arrive at, genesis)
        /// </summary>
        internal List<(LocationId, Journey<T>)> TargetStop { get; set; }

        /// <summary>
        /// The metrics that are used in the journeys
        /// </summary>
        internal T MetricFactory { get; set; }

        /// <summary>
        /// How to compare the metrics
        /// </summary>
        internal ProfiledMetricComparator<T> Comparator { get; set; }

        /// <summary>
        /// How long we should at least wait between two trains
        /// </summary>
        internal IOtherModeGenerator TransferPolicy { get; set; }

        /// <summary>
        /// How to walk from one stop to another, possibly between operators
        /// </summary>
        internal IOtherModeGenerator WalkPolicy { get; set; }

        /// <summary>
        /// A class filtering out connections which are useless to check
        /// </summary>
        internal IConnectionFilter Filter
        {
            get => _filter;
            set
            {
                if (value == null)
                {
                    _filter = null;
                    return;
                }

                value.CheckWindow(EarliestDeparture.ToUnixTime(), LastArrival.ToUnixTime());
                _filter = value;
            }
        }

        /// <summary>
        /// An example journey in order to filter out sub-optimal journeys.
        /// Optional
        /// </summary>
        internal Journey<T> ExampleJourney { get; set; }

        public ScanSettings(IEnumerable<TransitDb.TransitDbSnapShot> transitDbs, DateTime earliestDeparture,
            DateTime lastDeparture,
            T metricFactory, ProfiledMetricComparator<T> comparator, IOtherModeGenerator transferPolicy,
            IOtherModeGenerator walkPolicy, LocationId departureStop, LocationId targetStop)
            : this(transitDbs, earliestDeparture, lastDeparture, metricFactory, comparator, transferPolicy, walkPolicy,
                new List<LocationId> {departureStop}, new List<LocationId> {targetStop})
        {
        }


        internal ScanSettings(IEnumerable<TransitDb.TransitDbSnapShot> transitDbs, DateTime earliestDeparture,
            DateTime lastDeparture,
            T metricFactory, ProfiledMetricComparator<T> comparator, IOtherModeGenerator transferPolicy,
            IOtherModeGenerator walkPolicy, IEnumerable<LocationId> departureLocations, IEnumerable<LocationId> targetLocations)
            : this(transitDbs, earliestDeparture, lastDeparture, metricFactory, comparator, transferPolicy, walkPolicy,
                AddNullJourneys(departureLocations), AddNullJourneys(targetLocations))
        {
        }


        private static List<(LocationId, Journey<T>)> AddNullJourneys(IEnumerable<LocationId> locs)
        {
            var l = new List<(LocationId, Journey<T>)>();
            foreach (var loc in locs)
            {
                l.Add((loc, null));
            }

            return l;
        }


        internal ScanSettings(IEnumerable<TransitDb.TransitDbSnapShot> transitDbs,
            DateTime earliestDeparture, DateTime lastDeparture,
            T metricFactory, ProfiledMetricComparator<T> comparator, IOtherModeGenerator transferPolicy,
            IOtherModeGenerator walkPolicy, List<(LocationId, Journey<T>)> departureStop,
            List<(LocationId, Journey<T>)> targetLocation)
        {
            var cons = new List<IConnectionEnumerator>();
            var stops = new List<IStopsReader>();
            foreach (var tdb in transitDbs)
            {
                stops.Add(tdb.StopsDb.GetReader());
                cons.Add(tdb.ConnectionsDb.GetDepartureEnumerator());
            }

            Connections = ConnectionEnumeratorAggregator.CreateFrom(cons);
            StopsDbReader = StopsReaderAggregator.CreateFrom(transitDbs);
            EarliestDeparture = earliestDeparture;
            LastArrival = lastDeparture;
            MetricFactory = metricFactory;
            Comparator = comparator;
            TransferPolicy = transferPolicy;
            WalkPolicy = walkPolicy;
            DepartureStop = departureStop;
            TargetStop = targetLocation;
        }

        internal ScanSettings(IEnumerable<TransitDb.TransitDbSnapShot> snapshots, LocationId departureStop,
            LocationId arrivalStop,
            DateTime departureTime, DateTime arrivalTime, Profile<T> profile)
            : this(
                snapshots,
                departureTime, arrivalTime,
                profile.MetricFactory, profile.ProfileComparator,
                profile.InternalTransferGenerator, profile.WalksGenerator,
                departureStop, arrivalStop)
        {
        }

      


        internal void SanityCheck()
        {
            if (EarliestDeparture == DateTime.MinValue && LastArrival == DateTime.MinValue)
            {
                throw new ArgumentException(
                    "Both Earliest Departure time and Latest Arrival time are missing or MIN_VALUE. At least one should be given");
            }

            if (EarliestDeparture >= LastArrival)
            {
                throw new ArgumentException("The specified departure time is after the arrival time");
            }

            if (DepartureStop.Count == 0 && TargetStop.Count == 0)
            {
                throw new Exception("No departure nor arrival locations givens");
            }


            foreach (var dep in DepartureStop)
            {
                foreach (var target in TargetStop)
                {
                    if (!dep.Equals(target)) continue;


                    StopsDbReader.MoveTo(dep.Item1);
                    throw new ArgumentException("A departure location is the same as an arrival location: " +
                                                StopsDbReader.GlobalId);
                }
            }
        }
    }
}