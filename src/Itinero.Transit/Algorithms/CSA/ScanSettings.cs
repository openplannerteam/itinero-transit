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
        public StopsDb.StopsDbReader StopsDbReader { get; set; }
        public StopsDb StopsDb { get; set; }
        
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
        public List<((uint, uint), Journey<T>)> DepartureStop { get; set; }
        /// <summary>
        /// A list of possible arrival locations with possible arrival journeys
        /// Journeys should be in a backward order (..., then take to arrive at, genesis)
        /// </summary>
        public List<((uint, uint), Journey<T>)> TargetStop { get; set; }
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
            IOtherModeGenerator walkPolicy, (uint, uint) departureStop, (uint, uint) targetStop)
            : this(transitDb, earliestDeparture, lastDeparture, statsFactory, comparator, transferPolicy, walkPolicy,
                new List<(uint, uint)> {departureStop}, new List<(uint, uint)> {targetStop})
        {
        }

        
        public ScanSettings(TransitDb.TransitDbSnapShot transitDb, DateTime earliestDeparture, DateTime lastDeparture,
            T statsFactory, ProfiledStatsComparator<T> comparator, IOtherModeGenerator transferPolicy,
            IOtherModeGenerator walkPolicy, List<(uint, uint)>  departureLocations, List<(uint, uint)>  targetLocations)
            : this(transitDb, earliestDeparture, lastDeparture, statsFactory, comparator, transferPolicy, walkPolicy,
              AddNullJourneys(departureLocations), AddNullJourneys(targetLocations))
        {
        }


        private static List<((uint, uint), Journey<T>)> AddNullJourneys(IEnumerable<(uint, uint)> locs)
        {
            var l = new List<((uint, uint), Journey<T>)>();
            foreach (var loc in locs)
            {
                l.Add((loc, null));
            }
            return l;
        }
        
        
        public ScanSettings(TransitDb.TransitDbSnapShot transitDb, 
            DateTime earliestDeparture, DateTime lastDeparture,
            T statsFactory, ProfiledStatsComparator<T> comparator, IOtherModeGenerator transferPolicy,
            IOtherModeGenerator walkPolicy, List<((uint, uint), Journey<T>)> departureStop, List<((uint, uint), Journey<T>)> targetLocation)
        {
            Connections = transitDb.ConnectionsDb.GetDepartureEnumerator();
            StopsDb = transitDb.StopsDb;
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

        public ScanSettings(TransitDb.TransitDbSnapShot snapshot, (uint, uint) departureStop, (uint, uint) arrivalStop,
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