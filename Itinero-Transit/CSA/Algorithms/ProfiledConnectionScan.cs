using System;
using System.Collections.Generic;
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
    public class ProfiledConnectionScan<T> where T : IJourneyStats<T>
    {
        private readonly Uri _departureLocation, _targetLocation;
        private readonly T _statsFactory;
        private readonly StatsComparator<T> _profileComparator;

        /// <summary>
        /// When a journey is found from the departure station to the arrival station,
        /// it will have certain stats.
        /// Only journeys performing as good as this journey 
        /// </summary>
        private readonly ParetoFrontier<T> _paretoFront;

        private readonly IConnectionsProvider _connectionsProvider;


        /// <summary>
        /// Maps each stop onto a pareto front of journeys (with profiles).
        /// If the station isn't in the dictionary yet, this means no trip from this station has been already found.
        ///
        /// Also known as 'S' in the paper
        ///
        /// Note that the list is sorted in descending order (thus first departure in time last in the list)
        /// There can be multiple points which depart at the same time (but will have different arrival times and different other properties)
        /// </summary>
        private readonly Dictionary<Uri, ParetoFrontier<T>> _stationJourneys = new Dictionary<Uri, ParetoFrontier<T>>();

        /// <summary>
        /// Create a new ProfiledConnectionScan algorithm with the given parameters
        /// </summary>
        /// <param name="departureLocation">The URI-ID of the location where the traveller leaves</param>
        /// <param name="targetLocation">The URI-ID of the location where the traveller would like to go</param>
        /// <param name="connectionsProvider">The object providing connections from one or multiple transit operators</param>
        /// <param name="statsFactory">The object creating the statistics for each journey</param>
        /// <param name="profileComparator">An object comparing statistics which compares using profiles. Important:
        ///     This comparator should _not_ filter out journeys with a suboptimal time length, but only filter shadowed time travels.
        ///     (e.g. if a journey from starting at 10:00 and arriving at 11:00 is compared with a journey starting at 09:00 and arriving at 09:05,
        ///     no conclusions should be drawn.
        ///     This comparator is used in intermediate stops. Filtering away the trip 10:00 -> 11:00 at an intermediate stop
        ///     might remove a transfer that turned out to be optimal from the starting position.
        /// </param>
        /// <param name="paretoComparator">
        ///  An object comparing statistics which compares for optimal points as the user sees fit.
        ///     This comparator is used to filter the final list of journeys.
        ///     It is used to retain only the journeys with the least travel time, least number of transfers, cheapest cost, ...
        /// </param>
        public ProfiledConnectionScan(Uri departureLocation, Uri targetLocation,
            IConnectionsProvider connectionsProvider,IConnectionsProvider footpathProv,
            T statsFactory, StatsComparator<T> profileComparator, StatsComparator<T> paretoComparator)
        {
            _departureLocation = departureLocation;
            _targetLocation = targetLocation;
            _connectionsProvider = connectionsProvider;
            _statsFactory = statsFactory;
            _profileComparator = profileComparator;
            _paretoFront = new ParetoFrontier<T>(paretoComparator);
        }
        
        /// <summary>
        /// Calculate possible journeys from the given provider,
        /// where the Uri points to the timetable of the last allowed arrival at the destination station
        /// </summary>
        /// <returns></returns>
        public HashSet<Journey<T>> CalculateJourneys(DateTime earliestDeparture, DateTime lastArrival)
        {
            var tt = _connectionsProvider.GetTimeTable(_connectionsProvider.TimeTableIdFor(lastArrival));
            while (true)
            {
                var cons = tt.Connections();
                Log.Information($"Handling timetable{tt.StartTime():O}");
                for (var i = cons.Count - 1; i >= 0; i--)
                {
                    var c = cons[i];
                    if (c.DepartureTime() < earliestDeparture)
                    {
                        // We're done! Returning values
                        return _paretoFront.Frontier;
                    }

                    AddConnection(c);
                }

                tt = _connectionsProvider.GetTimeTable(tt.PreviousTable());
            }
        }

        /// <summary>
        /// Handles a connection backwards in time
        /// </summary>
        /// <param name="c"></param>
        private void AddConnection(IConnection c)
        {
            if (c.DepartureLocation().Equals(_targetLocation))
            {
                // We want to be here, lets not leave
                return;
            }

            if (c.ArrivalLocation().Equals(_departureLocation))
            {
                // We want to leave here, not arrive
                return;
            }

            if (c.ArrivalLocation().Equals(_targetLocation))
            {
                // We can arrive in our target location.
                // We create a new journey and add it
                var journey = new Journey<T>(_statsFactory.InitialStats(c), c.DepartureTime(), c);

                ConsiderJourney(c.DepartureLocation(), journey);
                return;
            }

            if (!_stationJourneys.ContainsKey(c.ArrivalLocation()))
            {
                // NO way out of the arrival station yet
                return;
            }

            var journeysToEnd = _stationJourneys[c.ArrivalLocation()];
            foreach (var j in journeysToEnd.Frontier)
            {
                var journey = j;
                if (c.ArrivalTime() > journey.Time)
                {
                    // We missed this connection
                    continue;
                }

                if (_connectionsProvider != null && !c.Trip().Equals(j.Connection.Trip()))
                {
                    // Create a transfer object, according to the transfer policy (if one is given)

                    // The transfer-policy expects two connections: the start and end connection
                    // We build our journey from end to start, thus this is the order we have to pass the arguments
                    var transferC = _connectionsProvider.CalculateInterConnection(c, j.Connection);
                    if (transferC == null)
                    {
                        // Transfer-policy deemed this transfer impossible
                        // We skip the connection
                        continue;
                    }

                    journey = new Journey<T>(j, transferC.DepartureTime(), transferC);
                }

                // Chaining to the start of the journey, not the end -> We keep track of the departure time (although the time is not actually used)
                var chained = new Journey<T>(journey, c.DepartureTime(), c);
                ConsiderJourney(c.DepartureLocation(), chained);
            }
        }

        ///  <summary>
        ///  This method is called when a new startStation can be reached with the given Journey J.
        /// 
        ///  The method will consider if this journey is profile optimal and should thus be included.
        ///  If it is not, it will be ignored.
        ///
        ///  If a pareto comparator is found and total journeys are already known,
        ///  the journey is also checked against the pareto frontier
        ///  </summary>
        ///  <param name="startStation"></param>
        ///  <param name="considered"></param>
        private void ConsiderJourney(Uri startStation, Journey<T> considered)
        {
            if (!_stationJourneys.ContainsKey(startStation))
            {
                _stationJourneys.Add(startStation, new ParetoFrontier<T>(_profileComparator));
            }

            var startJourneys = _stationJourneys[startStation];

            // Compare with the already known total journeys
            if (!_paretoFront.OnTheFrontier(considered))
            {
                // We won't be able to outperform an already known total route, especially because this is a partial route
                return;
            }

            startJourneys.AddToFrontier(considered); // List is still shared with the dictionary
            if (startStation.Equals(_departureLocation))
            {
                // The Frontier is what will be returned; the journeys here are constructed in reverse order
                // We already return them (it shouldn't matter anyway for the stats)
                _paretoFront.AddToFrontier(considered.Reverse());
            }
        }
    }
}