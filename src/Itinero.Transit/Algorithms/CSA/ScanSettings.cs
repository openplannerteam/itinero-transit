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
    public class ScanSettings<T> where T : IJourneyStats<T>
    {
        /// <summary>
        /// The snapshot on which the algorithms should be run
        /// </summary>
        public TransitDb.TransitDbSnapShot TransitDb { get; set; }
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
        public List<((uint, uint), Journey<T>)> DepartureLocation { get; set; }
        /// <summary>
        /// A list of possible arrival locations with possible arrival journeys
        /// Journeys should be in a backward order (..., then take to arrive at, genesis)
        /// </summary>
        public List<((uint, uint), Journey<T>)> TargetLocation { get; set; }
        public T StatsFactory { get; set; }
        public ProfiledStatsComparator<T> Comparator { get; set; }
        public IConnectionFilter Filter { get; set; }
        public IOtherModeGenerator TransferPolicy { get; set; }
        public IOtherModeGenerator WalkPolicy { get; set; }

        public ScanSettings(TransitDb.TransitDbSnapShot transitDb, DateTime earliestDeparture, DateTime lastDeparture,
            T statsFactory, ProfiledStatsComparator<T> comparator, IOtherModeGenerator transferPolicy,
            IOtherModeGenerator walkPolicy, (uint, uint) departureLocation, (uint, uint) targetLocation)
            : this(transitDb, earliestDeparture, lastDeparture, statsFactory, comparator, transferPolicy, walkPolicy,
                new List<(uint, uint)> {departureLocation}, new List<(uint, uint)> {targetLocation})
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
        
        
        public ScanSettings(TransitDb.TransitDbSnapShot transitDb, DateTime earliestDeparture, DateTime lastDeparture,
            T statsFactory, ProfiledStatsComparator<T> comparator, IOtherModeGenerator transferPolicy,
            IOtherModeGenerator walkPolicy, List<((uint, uint), Journey<T>)> departureLocation, List<((uint, uint), Journey<T>)> targetLocation)
        {
            TransitDb = transitDb;
            EarliestDeparture = earliestDeparture;
            LastArrival = lastDeparture;
            StatsFactory = statsFactory;
            Comparator = comparator;
            TransferPolicy = transferPolicy;
            WalkPolicy = walkPolicy;
            DepartureLocation = departureLocation;
            TargetLocation = targetLocation;
        }

        public void SanityCheck()
        {
            if (EarliestDeparture >= LastArrival)
            {
                throw new ArgumentException("The specified departure time is after the arrival time");
            }

            if (DepartureLocation.Count == 0 && TargetLocation.Count == 0)
            {
                throw new Exception("No departure nor arrival locations givens");
            }

            foreach (var dep in DepartureLocation)
            {
                foreach (var target in TargetLocation)
                {
                    if (!dep.Equals(target)) continue;
                    
                    
                    var reader = TransitDb.StopsDb.GetReader();
                    reader.MoveTo(dep.Item1);
                    throw new ArgumentException("A departure location is the same as an arrival location: "+reader.GlobalId);
                }
                
            }
            
        }
    }
}