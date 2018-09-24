using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// The ProfiledConnectionScan is a CSA that applies A* backward and builds profiles on how to reach a target stop.
    ///
    /// For each stop, a number of possible journeys to the destination are tracked - where each journey is a pareto-optimal option towards the destination.
    /// All connections are scanned (from the future to the past, in backward order) to update the journeys from stops.
    ///
    /// We stop when the time window has passed; after which we can give a number of pareto-optimal journeys to the traveller.
    /// 
    /// 
    /// </summary>
    public class ProfiledConnectionScan<T> where T : IJourneyStats
    {
        private readonly Uri _departureStation, _targetStation;
        private readonly T _statsFactory;
        private readonly IStatsComparator<T> _comparator;
        private readonly DateTime _earliestDeparture;

        private readonly HashSet<Journey> _emptyJourneys = new HashSet<Journey>();

        /// <summary>
        /// Maps each stop onto a pareto front of journeys.
        /// If the station isn't in the dictionary yet, this means no trip from this station has been already found.
        ///
        /// Also known as 'S' in the paper
        /// </summary>
        private readonly Dictionary<Uri, HashSet<Journey>> _stationJourneys = new Dictionary<Uri, HashSet<Journey>>();

        public ProfiledConnectionScan(Uri departureStation, Uri targetStation, DateTime earliestDeparture,
            T statsFactory,
            IStatsComparator<T> comparator)
        {
            _departureStation = departureStation;
            _targetStation = targetStation;
            _statsFactory = statsFactory;
            _comparator = comparator;
            _earliestDeparture = earliestDeparture;
        }

        /// <summary>
        /// Calculate possible journeys from the given provider,
        /// where the Uri points to the timetable of the last allowed arrival at the destination station
        /// </summary>
        /// <returns></returns>
        public HashSet<Journey> CalculateJourneys(Uri lastArrival)
        {
            var tt = new TimeTable(lastArrival);
            tt.Download();

            tt.Graph.Reverse();

            foreach (var c in tt.Graph)
            {
                if (c.DepartureTime < _earliestDeparture)
                {
                    // We're done! Returning values
                    return _stationJourneys.GetValueOrDefault(_departureStation, _emptyJourneys);
                }

                AddConnection(c);
            }

            // ReSharper disable once TailRecursiveCall
            return CalculateJourneys(tt.Prev);
        }

        /// <summary>
        /// Handles a connection backwards in time
        /// </summary>
        /// <param name="c"></param>
        private void AddConnection(Connection c)
        {
            if (c.ArrivalStop.Equals(_targetStation))
            {
                Log.Information("Found a way in: " + c);
                // We keep track of the departure times here!
                var journey = new Journey(_statsFactory.InitialStats(c), c.DepartureTime.AddSeconds(c.DepartureDelay),
                    c);
                ConsiderJourney(c.DepartureStop, journey);
                return;
            }

            var journeysToEnd = _stationJourneys.GetValueOrDefault(c.DepartureStop, _emptyJourneys);
            foreach (var j in journeysToEnd)
            {
                
                if(c.ArrivalTime.AddSeconds(c.ArrivalDelay) > j.Time)
                {
                    // We missed this journey
                    continue;
                }
                
                // Chaining to the start of the journey, not the end -> We keep track of the departure time (although the time is not actually used)
                var chained = new Journey(j, c.DepartureTime.AddSeconds(c.DepartureDelay), c);
                ConsiderJourney(c.DepartureStop, chained);
            }
        }

        /// <summary>
        /// This method is called when a new startStation can be reached with the given Journey J.
        ///
        /// The method will consider if this journey is pareto optimal and should thus be included.
        /// If it is not, it will be ignored.
        /// </summary>
        /// <param name="startStation"></param>
        /// <param name="considered"></param>
        private void ConsiderJourney(Uri startStation, Journey considered)
        {
            
            if (!_stationJourneys.ContainsKey(startStation))
            {
                _stationJourneys.Add(startStation, new HashSet<Journey>());
            }

            var startJourneys = _stationJourneys[startStation];

            Log.Information($"Considering journey {considered}");

            var toRemove = new HashSet<Journey>();
            foreach (var journey in startJourneys)
            {
                var comparison = _comparator.ADominatesB((T) journey.Stats, (T) considered.Stats);
                // ReSharper disable once InvertIf
                if (comparison == -1)
                {
                    // The considered journey is dominated and thus useless
                    Log.Information($"Dominated by {journey}");
                    return;
                }

                if (comparison == 1)
                {
                    // The considered journey dominates the current journey
                    toRemove.Add(journey);
                }
                
                // The other cases are 0 (both are the same) of MaxValue (both are not comparable)
                // Then we keep both
            }


            foreach (var journey in toRemove)
            {
                startJourneys.Remove(journey);
            }
            

            Log.Information("Added journey for " + considered.Connection);
            startJourneys.Add(considered);
        }
    }
}