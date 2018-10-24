using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Itinero_Transit.CSA.ConnectionProviders;
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
        /// <summary>
        /// Represents multiple 'target' stations, or walking transfers to the last stop.
        /// The key of this dictionary is where this footpath can be taken (thus the contained connections.DepartureStation)
        /// </summary>
        private readonly Dictionary<string, IContinuousConnection> _footpathsOut
            = new Dictionary<string, IContinuousConnection>();

        private readonly HashSet<string> _departureLocations = new HashSet<string>();

        private readonly Profile<T> _profile;

        private readonly StatsComparator<T> _profileComparator;

        private readonly IConnectionsProvider _connectionsProvider;
        private readonly DateTime _earliestDeparture, _lastArrival;

        /// <summary>
        /// Maps each stop onto a pareto front of journeys (with profiles).
        /// If the station isn't in the dictionary yet, this means no trip from this station has been already found.
        ///
        /// Also known as 'S' in the paper
        ///
        /// Note that the list is sorted in descending order (thus first departure in time last in the list)
        /// There can be multiple points which depart at the same time (but will have different arrival times and different other properties)
        /// </summary>
        private readonly Dictionary<string, ParetoFrontier<T>> _stationJourneys =
            new Dictionary<string, ParetoFrontier<T>>();

        ///  <summary>
        ///  Create a new ProfiledConnectionScan algorithm.
        /// 
        ///  The profile is used for quite some parameters:
        /// 
        ///  - connectionsProvider: The object providing connections of a transit operator
        ///  - LocationsProvider: provides mapping of a locationID onto coordinates and searches close by stops
        ///  - FootpathGenerator: provides (intermodal) transfers 
        ///  - statsFactory: The object creating the statistics for each journey
        ///  - profileComparator: An object comparing statistics which compares using profiles. Important:
        ///      This comparator should _not_ filter out journeys with a suboptimal time length, but only filter shadowed time travels.
        ///      (e.g. if a journey from starting at 10:00 and arriving at 11:00 is compared with a journey starting at 09:00 and arriving at 09:05,
        ///      no conclusions should be drawn.
        ///      This comparator is used in intermediate stops. Filtering away the trip 10:00 -> 11:00 at an intermediate stop
        ///      might remove a transfer that turned out to be optimal from the starting position.
        ///  </summary>
        ///  <param name="departureLocation">The URI-ID of the location where the traveller leaves</param>
        ///  <param name="targetLocation">The URI-ID of the location where the traveller would like to go</param>
        /// <param name="profile">The profile containing all the parameters as described above</param>
        /// <param name="earliestDeparture">The earliest moment that the traveller starts his/her travel</param>
        /// <param name="lastArrival">When the traveller wants to arrive at last</param>
        public ProfiledConnectionScan(Uri departureLocation, Uri targetLocation,
            DateTime earliestDeparture, DateTime lastArrival,
            Profile<T> profile)
        {
            if (targetLocation.Equals(departureLocation))
            {
                throw new ArgumentException("Departure and target location are the same");
            }

            _profile = profile;
            _earliestDeparture = earliestDeparture;
            _lastArrival = lastArrival;
            _footpathsOut.Add(targetLocation.ToString(), new WalkingConnection(targetLocation, lastArrival));
            _connectionsProvider = profile.ConnectionsProvider;
            _profileComparator = profile.ProfileCompare;
            _departureLocations.Add(departureLocation.ToString());
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public ProfiledConnectionScan(IEnumerable<Uri> departureLocations,
            IEnumerable<IContinuousConnection> targetLocations,
            DateTime earliestDeparture, DateTime lastArrival,
            Profile<T> profile)
        {
            if (!departureLocations.Any())
            {
                throw new ArgumentException("No departure locations given. Cannot run PCS in this case");
            }

            if (!targetLocations.Any())
            {
                throw new ArgumentException("No target locations are given, Cannot run PCS in this case");
            }

            foreach (var target in targetLocations)
            {
                _footpathsOut.Add(target.DepartureLocation().ToString(), target);
            }

            foreach (var source in departureLocations)
            {
                _departureLocations.Add(source.ToString());
            }

            _earliestDeparture = earliestDeparture;
            _lastArrival = lastArrival;

            _connectionsProvider = profile.ConnectionsProvider;
            _profileComparator = profile.ProfileCompare;
            _profile = profile;
        }

        /// <summary>
        /// Calculate possible journeys from the given provider,
        /// where the Uri points to the timetable of the last allowed arrival at the destination station
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, IEnumerable<Journey<T>>> CalculateJourneys()
        {
            var tt = _profile.ConnectionsProvider.GetTimeTable(
                _profile.ConnectionsProvider.TimeTableIdFor(_lastArrival));
            while (true)
            {
                var cons = tt.Connections();
                Log.Information($"Handling timetable{tt.StartTime():O}");
                for (var i = cons.Count - 1; i >= 0; i--)
                {
                    var c = cons[i];
                    if (c.DepartureTime() < _earliestDeparture)
                    {
                        // We're done! Returning values
                        var result = new Dictionary<string, IEnumerable<Journey<T>>>();
                        foreach (var loc in _departureLocations)
                        {
                            var frontier
                                = _stationJourneys.GetValueOrDefault(loc, null)?.Frontier
                                  ?? new HashSet<Journey<T>>();
                            result.Add(loc, frontier);
                        }

                        return result;
                    }

                    AddConnection(c);
                }

                tt = _connectionsProvider.GetTimeTable(tt.PreviousTable());
            }
        }

        /// <summary>
        /// Handles a connection backwards in time
        /// </summary>
        private void AddConnection(IConnection c)
        {
            if (_footpathsOut.ContainsKey(c.ArrivalLocation().ToString()))
            {
                // We can arrive in one of our target locations.
                // We create a new journey and add it
                var endConn = _footpathsOut[c.ArrivalLocation().ToString()];
                var diff = (c.ArrivalTime() - endConn.DepartureTime()).TotalSeconds;
                endConn = endConn.MoveTime((int) diff);
                var initialJourney = new Journey<T>(
                    _profile.StatsFactory.InitialStats(endConn), endConn);

                var journey = new Journey<T>(initialJourney, c);

                ConsiderJourney(c.DepartureLocation().ToString(), journey);
                return;
            }

            if (!_stationJourneys.ContainsKey(c.ArrivalLocation().ToString()))
            {
                // NO way out of the arrival station yet
                return;
            }

            var journeysToEnd = _stationJourneys[c.ArrivalLocation().ToString()];
            foreach (var j in journeysToEnd.Frontier)
            {
                var journey = j;
                if (c.ArrivalTime() > j.Connection.DepartureTime())
                {
                    // We missed this connection
                    continue;
                }

                // TODO REMOVE CHEAT (TripID -> Route)
                if (_profile.FootpathTransferGenerator != null && !c.Route().Equals(j.GetLastTripId()))
                {
                    // Create a transfer object, according to the transfer policy (if one is given)

                    // The transfer-policy expects two connections: the start and end connection
                    // We build our journey from end to start, thus this is the order we have to pass the arguments
                    var transferC = _profile.CalculateInterConnection(c, j.Connection);
                    if (transferC == null)
                    {
                        // Transfer-policy deemed this transfer impossible
                        // We skip the connection
                        continue;
                    }

                    journey = new Journey<T>(j, transferC);
                }

                // Chaining to the start of the journey, not the end -> We keep track of the departure time (although the time is not actually used)
                var chained = new Journey<T>(journey, c);
                ConsiderJourney(c.DepartureLocation().ToString(), chained);
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
        private void ConsiderJourney(string startStation, Journey<T> considered)
        {
            if (!_stationJourneys.ContainsKey(startStation))
            {
                _stationJourneys.Add(startStation, new ParetoFrontier<T>(_profileComparator));
            }

            var startJourneys = _stationJourneys[startStation];

            startJourneys.AddToFrontier(considered); // List is still shared with the dictionary
        }

        /// <summary>
        /// Returns the profile frontier for a certain departure station.
        /// Should only be used to debug
        /// </summary>
        /// <param name="departureStation"></param>
        /// <returns></returns>
        public ParetoFrontier<T> GetProfileFor(Uri departureStation)
        {
            return _stationJourneys[departureStation.ToString()];
        }
    }
}