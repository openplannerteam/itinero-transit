using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Algorithms.CSA
{
    /// <summary>
    /// Scansettings is a small object keeping track of all common parameters to run a scan
    /// </summary>
    internal class ScanSettings<T> where T : IJourneyStats<T>
    {
        private IConnectionFilter _filter;

        /// <summary>
        /// The connections that can be used, orderd by departure time
        /// </summary>
        public IConnectionEnumerator Connections { get; set; }
        public IStopsReader StopsDbReader { get; set; }
        
        /// <summary>
        /// The earliest time the traveller wants to depart.
        /// In the case of LAS, this is a timeout if no route is found (or to calculate isochrone lines)
        /// </summary>
        public DateTime EarliestDeparture { get; set; }
        /// <summary>
        /// The latest time the traveller wants to arrive.
        /// In case of EAS, this acts as a timeout if no route is found (or to calculate isochrone lines)
        /// </summary>
        public DateTime LastArrival { get; set; }
        
        /// <summary>
        /// A list of possible departure locations with possible departure journeys.
        /// Journeys should be in a forward order (genesis, then take...)
        /// </summary>
        public List<(LocationId, Journey<T>)> DepartureStop { get; set; }
        /// <summary>
        /// A list of possible arrival locations with possible arrival journeys
        /// Journeys should be in a backward order (..., then take to arrive at, genesis)
        /// </summary>
        public List<(LocationId, Journey<T>)> TargetStop { get; set; }
        /// <summary>
        /// The statistics that are used in the journeys
        /// </summary>
        public T StatsFactory { get; set; }
        /// <summary>
        /// How to compare the statistics
        /// </summary>
        public ProfiledStatsComparator<T> Comparator { get; set; }
        /// <summary>
        /// How long we should at least wait between two trains
        /// </summary>
        public IOtherModeGenerator TransferPolicy { get; set; }
        /// <summary>
        /// How to walk from one stop to another, possibly between operators
        /// </summary>
        public IOtherModeGenerator WalkPolicy { get; set; }

        /// <summary>
        /// A class filtering out connections which are useless to check
        /// </summary>
        public IConnectionFilter Filter
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
        public Journey<T> ExampleJourney { get; set; }

        public ScanSettings(TransitDb.TransitDbSnapShot transitDb, DateTime earliestDeparture, DateTime lastDeparture,
            T statsFactory, ProfiledStatsComparator<T> comparator, IOtherModeGenerator transferPolicy,
            IOtherModeGenerator walkPolicy, LocationId departureStop, LocationId targetStop)
            : this(transitDb, earliestDeparture, lastDeparture, statsFactory, comparator, transferPolicy, walkPolicy,
                new List<LocationId> {departureStop}, new List<LocationId> {targetStop})
        {
        }

        
        public ScanSettings(TransitDb.TransitDbSnapShot transitDb, DateTime earliestDeparture, DateTime lastDeparture,
            T statsFactory, ProfiledStatsComparator<T> comparator, IOtherModeGenerator transferPolicy,
            IOtherModeGenerator walkPolicy, List<LocationId>  departureLocations, List<LocationId>  targetLocations)
            : this(transitDb, earliestDeparture, lastDeparture, statsFactory, comparator, transferPolicy, walkPolicy,
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
        
        
        public ScanSettings(TransitDb.TransitDbSnapShot transitDb, 
            DateTime earliestDeparture, DateTime lastDeparture,
            T statsFactory, ProfiledStatsComparator<T> comparator, IOtherModeGenerator transferPolicy,
            IOtherModeGenerator walkPolicy, List<(LocationId, Journey<T>)> departureStop, List<(LocationId, Journey<T>)> targetLocation)
        {
            Connections = transitDb.ConnectionsDb.GetDepartureEnumerator();
            StopsDbReader = transitDb.StopsDb.GetReader();
            EarliestDeparture = earliestDeparture;
            LastArrival = lastDeparture;
            StatsFactory = statsFactory;
            Comparator = comparator;
            TransferPolicy = transferPolicy;
            WalkPolicy = walkPolicy;
            DepartureStop = departureStop;
            TargetStop = targetLocation;
        }

        public ScanSettings(TransitDb.TransitDbSnapShot snapshot, LocationId departureStop, LocationId arrivalStop,
            DateTime departureTime, DateTime arrivalTime, Profile<T> profile)
        : this(
            snapshot,
            departureTime, arrivalTime, 
            profile.StatsFactory, profile.ProfileComparator, 
            profile.InternalTransferGenerator, profile.WalksGenerator, 
            departureStop, arrivalStop )
        {
        }

        public void SanityCheck()
        {
            if (EarliestDeparture == DateTime.MinValue && LastArrival == DateTime.MinValue)
            {
                throw new ArgumentException("Both Earliest Departure time and Latest Arrival time are missing or MIN_VALUE. At least one should be given");
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
                    throw new ArgumentException("A departure location is the same as an arrival location: "+StopsDbReader.GlobalId);
                }
                
            }
            
        }
    }
}