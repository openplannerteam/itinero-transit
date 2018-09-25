using System;
using System.Collections.Generic;
using Itinero_Transit.LinkedData;
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
        private readonly Uri _departureLocation, _targetLocation;
        private readonly T _statsFactory;
        private readonly IStatsComparator<T> _comparator;
        private readonly DateTime _earliestDeparture;

        private readonly List<Journey> _emptyJourneys = new List<Journey>();

        /// <summary>
        /// Maps each stop onto a pareto front of journeys.
        /// If the station isn't in the dictionary yet, this means no trip from this station has been already found.
        ///
        /// Also known as 'S' in the paper
        ///
        /// Note that the list is sorted in descending order (thus first departure in time last in the list)
        /// There can be multiple points which depart at the same time (but will have different arrival times and different other properties)
        /// </summary>
        private readonly Dictionary<Uri, List<Journey>> _stationJourneys = new Dictionary<Uri, List<Journey>>();

        public ProfiledConnectionScan(Uri departureLocation, Uri targetLocation, DateTime earliestDeparture,
            T statsFactory,
            IStatsComparator<T> comparator)
        {
            _departureLocation = departureLocation;
            _targetLocation = targetLocation;
            _statsFactory = statsFactory;
            _comparator = comparator;
            _earliestDeparture = earliestDeparture;
        }

        /// <summary>
        /// Calculate possible journeys from the given provider,
        /// where the Uri points to the timetable of the last allowed arrival at the destination station
        /// </summary>
        /// <returns></returns>
        public List<Journey> CalculateJourneys(Uri lastArrival)
        {
            var tt = new TimeTable(lastArrival);
            tt.Download();

            tt.Graph.Reverse();

            foreach (var c in tt.Graph)
            {
                if (c.DepartureTime < _earliestDeparture)
                {
                    // We're done! Returning values
                    _dumpStationJourneys();
                    return _stationJourneys.GetValueOrDefault(_departureLocation, _emptyJourneys);
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
        private void AddConnection(IConnection c)
        {
            if (c.DepartureLocation().Equals(_targetLocation))
            {
                // We want to be here! Lets not leave ;)
                return;
            }

            if (c.ArrivalLocation().Equals(_targetLocation))
            {
                // We keep track of the departure times here!
                var journey = new Journey(_statsFactory.InitialStats(c), c.DepartureTime(), c);
                ConsiderJourney(c.DepartureLocation(), journey);
                return;
            }

            var journeysToEnd = _stationJourneys.GetValueOrDefault(c.ArrivalLocation(), _emptyJourneys);
            foreach (var j in journeysToEnd)
            {
                if (c.ArrivalTime() > j.Time)
                {
                    // We missed this connection
                    continue;
                }

                // Chaining to the start of the journey, not the end -> We keep track of the departure time (although the time is not actually used)
                var chained = new Journey(j, c.DepartureTime(), c);
                ConsiderJourney(c.DepartureLocation(), chained);
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
                _stationJourneys.Add(startStation, new List<Journey>());
            }

            var startJourneys = _stationJourneys[startStation];

            var log = startStation.Equals(_departureLocation) ||
                      startStation.Equals(Stations.GetId("Gent-Sint-Pieters"));
            if (log)
                Log.Information($"Considering journey {considered}");

            foreach (var journey in startJourneys)
            {
                var comparison = _comparator.ADominatesB((T) journey.Stats, (T) considered.Stats);
                // ReSharper disable once InvertIf
                if (comparison == -1)
                {
                    if (log) Log.Information($"Dominated by {journey}");
                    // The considered journey is dominated and thus useless
                    return;
                }

                // The other cases are 0 (both are the same) of MaxValue (both are not comparable)
                // Then we keep both
            }


            if (log) Log.Information("Added journey for " + considered.Connection);
            startJourneys.Add(considered); // List is still shared with the dictionary
        }

        private void _dumpStationJourneys()
        {
            var focus = new List<string>()
            {
                "Brugge",
                "Gent-Sint-Pieters",
                "Brussel-Centraal/Bruxelles-Central",
                "Brussel-Noord/Bruxelles-Nord"
            };
            foreach (var kv in focus)
            {
                var uri = Stations.GetId(kv);
                var journeys = "";
                if (!_stationJourneys.ContainsKey(uri))
                {
                    continue;
                }

                foreach (var journey in _stationJourneys[uri])
                {
                    journeys += ", " + journey;
                }

                Log.Information(
                    $"Journeys from {kv} to {Stations.GetName(_targetLocation)} are:\n -----------------------------------\n" +
                    $"{journeys}");
            }
        }
    }
}