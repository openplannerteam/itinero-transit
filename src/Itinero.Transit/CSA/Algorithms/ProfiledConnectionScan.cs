using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Itinero.Algorithms.PriorityQueues;

namespace Itinero.Transit
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


        /// <summary>
        /// When arriving at bus stop, we can walk to e.g. the close by train platform.
        /// We model these intermodal transfers as a connection which leaves the bus stop the moment that the a taken bus arrives.
        /// This way, weird extra objects are avoided.
        ///
        /// In order to maintain correctness, we feed these connections only to the algorithm when they are due. In the meantime,
        /// they are stored in this queue
        ///
        /// Note that the queue is kept in DESCENDING order
        /// </summary>
        private readonly BinaryHeap<IConnection> _queue = new BinaryHeap<IConnection>();

        /// <summary>
        /// Walking connections from the actual starting point to a nearby stop.
        /// Indexed by the arrival-location of the connections;
        /// also see the analogous _footpathsOut
        /// </summary>
        private readonly Dictionary<string, IContinuousConnection> _footpathsIn
            = new Dictionary<string, IContinuousConnection>();


        /// <summary>
        ///The profile of the traveller: which options he prefers
        /// </summary>
        private readonly Profile<T> _profile;

        private readonly ProfiledStatsComparator<T> _profileComparator;

        private readonly IConnectionsProvider _connectionsProvider;
        private readonly DateTime _earliestDeparture, _lastArrival;

        /// <summary> Maps each stop onto a list of non-dominated journeys.
        /// The list is ordered from latest to earliest departure.
        ///
        /// If a journey is considered, it should arrive sooner then the last element in the list.
        /// 
        /// If the station isn't in the dictionary yet, this means no trip from this station has been already found.
        ///
        /// Also known as 'S' in tOhe paper
        ///
        /// </summary>
        private readonly Dictionary<string, List<Journey<T>>> _stationJourneys =
            new Dictionary<string, List<Journey<T>>>();

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

            _profile = profile.MemoizingPathsProfile();
            _earliestDeparture = earliestDeparture;
            _lastArrival = lastArrival;
            _footpathsOut.Add(targetLocation.ToString(), new WalkingConnection(targetLocation, lastArrival));
            _connectionsProvider = profile.ConnectionsProvider;
            _profileComparator = profile.ProfileCompare;

            var genesis = new WalkingConnection(departureLocation, earliestDeparture);
            _footpathsIn.Add(genesis.ArrivalLocation().ToString(), genesis);
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        // ReSharper disable once UnusedMember.Global
        public ProfiledConnectionScan(IEnumerable<Uri> departureLocations,
            IEnumerable<Uri> targetLocations,
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


            foreach (var source in departureLocations)
            {
                _footpathsIn.Add(source.ToString(), new WalkingConnection(source, earliestDeparture));
            }

            foreach (var target in targetLocations)
            {
                _footpathsOut.Add(target.ToString(), new WalkingConnection(target, lastArrival));
            }

            _earliestDeparture = earliestDeparture;
            _lastArrival = lastArrival;
            _profile = profile;
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public ProfiledConnectionScan(IEnumerable<IContinuousConnection> departureLocations,
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
                _footpathsIn.Add(source.ArrivalLocation().ToString(), source);
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
            IConnection c = null;
            do
            {
                var cons = tt.ConnectionsReversed();
                foreach (var conn in cons)
                {
                    // The list is sorted by departure time
                    // The algorithm starts with the highest departure time and goes down

                    c = conn;
                    while (_queue.Count > 0 && _queue.Peek().DepartureTime() >= c.DepartureTime())
                    {
                        // We have an interlink on the queue that should be taken care of first
                        var walk = _queue.Pop();
                        AddConnection(walk);
                    }

                    AddConnection(c);
                }

                tt = _connectionsProvider.GetTimeTable(tt.PreviousTable());
            } while (c != null && c.DepartureTime() >= _earliestDeparture);


            // Post processing
            // Prepare a neat dictionary for each final destination for the end user
            var departureLocations = new HashSet<string>();
            foreach (var inConKey in _footpathsIn.Keys)
            {
                departureLocations.Add(_footpathsIn[inConKey].DepartureLocation().ToString());
            }

            var result = new Dictionary<string, IEnumerable<Journey<T>>>();
            foreach (var loc in departureLocations)
            {
                var frontier
                    = _stationJourneys.GetValueOrDefault(loc, new List<Journey<T>>());
                result.Add(loc, frontier);
            }

            return result;
        }

        /// <summary>
        /// Handles a connection backwards in time
        /// </summary>
        private void AddConnection(IConnection c)
        {
            // 1) Handle outgoing connections, they provide the first entries in 
            // _stationJourneys
            if (_footpathsOut.ContainsKey(c.ArrivalLocation().ToString()))
            {
                // We can arrive in one of our target locations.
                var walk = _footpathsOut[c.ArrivalLocation().ToString()];
                if (!walk.DepartureLocation().Equals(walk.ArrivalLocation()))
                {
                    // We arrive in a stop, from which we can walk to our target
                    // This implies that there is a 'walking connection'
                    // which leaves at the arrival time of C
                    var diff = (c.ArrivalTime() - walk.DepartureTime()).TotalSeconds;
                    walk = walk.MoveTime((int) diff);
                    if (walk.ArrivalTime() <= _lastArrival)
                    {
                        var journey = new Journey<T>(_profile.StatsFactory.InitialStats(walk), walk);

                        ConsiderJourney(journey);
                        // The connection itself: we leave that one to be handled by the rest of the code
                    }
                }
                else
                {
                    // NO target walks; we are at one of our destinations
                    // We can add this connection 'as is' to the stationJourneys

                    ConsiderJourney(new Journey<T>(
                        _profile.StatsFactory.InitialStats(c), c));
                    return;
                }

                // We still handle the connection as the rest of the journey
                // Perhaps the bus will continue to drive us closer to the station
            }

            // 2) Can the connection be used? If not, skip
            if (!_stationJourneys.ContainsKey(c.ArrivalLocation().ToString()))
            {
                // NO way out of the arrival station yet
                return;
            }


            // 3) Could this connection be reachable by foot, from the departure location?
            if (_footpathsIn.ContainsKey(c.DepartureLocation().ToString()))
            {
                // We have found a footpath in; thus the stop where the connection C will depart from,
                // can be reached from the actual departure location by Walking.
                // We model this as a new connection and queue it for pickup to the final destination
                var footpath = _footpathsIn[c.DepartureLocation().ToString()];
                if (!footpath.DepartureLocation().Equals(footpath.ArrivalLocation()))
                {
                    // Of course, the 'walking connection' should not be a trivial genesis connection
                    var diff = (c.DepartureTime() - footpath.ArrivalTime()).TotalSeconds;
                    footpath = footpath.MoveTime(diff);
                    _queue.Push(footpath, footpath.DepartureTime().Ticks);
                }
                // ELSE: we don't need to do anything, the start location is registered by the resting code

                // Note that we continue to handle the connection to consider the journey:
                // There could be a stop that is closer to the target!
            }


            var journeysToEnd = _stationJourneys[c.ArrivalLocation().ToString()];

            // Will be true if this connection is taken in at least one journey
            // If the connection is taken at least once, we should calculate the walking connections into the departure station
            bool journeyAdded = false;
            foreach (var j in journeysToEnd)
            {
                var journey = j;
                if (c.ArrivalTime() > j.Connection.DepartureTime())
                {
                    // We missed this connection
                    continue;
                }

                if (_profile.FootpathTransferGenerator != null
                    && !Equals(c.Trip(), j.GetLastTripId())
                    && !(c is IContinuousConnection || j.Connection is IContinuousConnection)
                )
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
                journeyAdded |= ConsiderJourney(chained);
            }

            if (journeyAdded)
            {
                ConsiderInterstopWalks(c);
            }
        }


        ///   <summary>
        ///   This method is called when a new startStation can be reached with the given Journey `considered`.
        ///  
        ///   The method will consider if this journey is profile optimal and should thus be included.
        ///   If it is not, it will be ignored.
        ///
        /// Returns True if the journey was effectiviely added
        ///   </summary>
        /// <param name="considered"></param>
        private bool ConsiderJourney(Journey<T> considered)
        {
            var startStation = considered.Connection.DepartureLocation().ToString();
            if (!_stationJourneys.ContainsKey(startStation))
            {
                _stationJourneys.Add(startStation, new List<Journey<T>> {considered});
                return true;
            }
            // We actually add the journey to the list if it is not dominated
            // Because the connections are scanned in decreasing departure time, there cannot be a connection with an earlier departure time
            // It is therefore sufficient for the domination test to look at the earliest pair q already in the array.


            // As
            // - The list of stationJourneys has the soonest departures on the back
            // - Connections are handled from later to earlier departure time
            // The _last_ elements in the list will have the _earliest_ departure times.
            //
            // Because a profiled stats comparator will always compare the start time of the journey,
            // we _only_ have to do a domination check against the last few elements 
            var frontier = _stationJourneys[startStation];

            var i = frontier.Count;
            while (i > 0 && frontier[i - 1].Connection.DepartureTime() == considered.Connection.DepartureTime())
            {
                i--;
                var guard = frontier[i];
                var duelResult = _profileComparator.ADominatesB(guard, considered);
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (duelResult)
                {
                    case -1:
                        // The guard wins and clearly dominates the considered journey
                        // The considered journey didn't make the cut: we don't add it
                        return false;
                    case 0:
                        // There is no considerable improvement
                        return false;
                    case 1:
                        // The guard loses and is dominated by the journey
                        // The guard is removed
                        frontier.RemoveAt(i);
                        i--;
                        break;
                    case int.MaxValue:
                        // They do not dominate each other
                        // We continue with the rest of the loop
                        break;
                }
            }

            // The last boss battle
            // We compare with the previous element
            // This is to filter out journeys which depart long before but also arrive after the previously added journey
            if (frontier.Count > 0)
            {
                var lastGuard = frontier[frontier.Count - 1];
                var duel = _profileComparator.ADominatesB(lastGuard, considered);
                if (duel < 0)
                {
                    return false;
                }

                // 0,1 and MAX_VALUE can never occur:
                // The start moments are different and in favour of the lastGuard
            }

            // If the considered journey has survived up to this point, it deserves its position in the frontier
            frontier.Add(considered);
            return true;
        }

        /// <summary>
        /// When an connection departing from location D (at time Dt) is taken, this implies that that
        /// walks arriving at D can be taken as well, if they arrive at time Dt.
        ///
        /// This method creates these walking connections for the journey and adds them to the queue
        /// </summary>
        private void ConsiderInterstopWalks(IConnection c)
        {
            // We can reach the target station from the departure station of the current journey
            // This also means that we can reach the target station via close by PT stops
            // We get all connections to this station and queue them
            if (_profile.IntermodalStopSearchRadius == 0)
            {
                // Early abort if searching close by stops is disabled
                return;
            }

            if (c is WalkingConnection)
            {
                // We've already done our share of walking
                return;
            }

            var walks = _profile.WalkFromCloseByStops(
                c.DepartureTime(),
                _profile.GetCoordinateFor(c.DepartureLocation()),
                _profile.IntermodalStopSearchRadius);
            foreach (var walk in walks)
            {
                if (walk.DepartureLocation().Equals(walk.ArrivalLocation()))
                {
                    continue;
                }


                // Ticks will increase. As the priority is increased, they are returned earlier
                _queue.Push(walk, walk.DepartureTime().Ticks);
            }
        }

        /// <summary>
        /// Returns the profile frontier for a certain departure station.
        /// Should only be used to debug
        /// </summary>
        /// <param name="departureStation"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Global
        public List<Journey<T>> GetProfileFor(Uri departureStation)
        {
            return _stationJourneys[departureStation.ToString()];
        }
    }
}