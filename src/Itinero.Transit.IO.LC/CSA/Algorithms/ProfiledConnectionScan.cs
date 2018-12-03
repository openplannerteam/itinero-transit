using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Itinero.IO.LC
{
    /// <summary>
    /// The ProfiledConnectionScan is a CSA that applies A* backward and builds profiles on how to reach a target stop.
    ///
    /// For each stop, a number of possible journeys to the destination are tracked - where each journey is a pareto-optimal option towards the destination.
    /// All connections are scanned (from the future to the past,  in backward order) to update the journeys from stops.
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
        /// Walking connections from the actual starting point to a nearby stop.
        /// Indexed by the arrival-location of the connections;
        /// also see the analogous _footpathsOut
        /// </summary>
        private readonly Dictionary<string, IContinuousConnection> _footpathsIn
            = new Dictionary<string, IContinuousConnection>();


        /// <summary>
        /// Keeps track of what stops are already reached by the backwards A*
        /// Used for the intermodal transfers
        /// </summary>
        private readonly ActiveLocationTracker<T> _knownLocations;

        /// <summary>
        ///The profile of the traveller: the preferred options, allowed operators, ...
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
        /// Also known as 'S' in the paper
        ///
        /// </summary>
        private readonly Dictionary<string, ParetoFrontier<T>> _stationJourneys =
            new Dictionary<string, ParetoFrontier<T>>();


        // Placeholder empty frontier; used when a frontier is needed but not present.
        private readonly ParetoFrontier<T> _empty;

        /// <summary>
        ///
        /// When sitting in a certain connection, gives what journey will take you
        /// to the destination the fastest. This might imply making a transfer earlier on.
        ///
        /// As it turns out, having this dict is quite essential.
        /// Consider the following:
        ///
        /// We are at point A and want to go to point C.
        ///
        /// There is an intermediate station B. A train travels from B to C, a bus travels from A, to B, to C at the following timings:
        ///
        /// Train:
        /// B at 10:00
        /// Arrives at C at 11:00
        ///
        /// Bus:
        /// Departs at A at 9:30 and 8:30
        /// Departs at B at 9:59 (and an hour earlier, 8:59)
        /// Arrives at C at 11:15
        ///
        /// _Without_ trip tracking, PCS will construct a profile at B:
        /// B: take the train at 10, arrive at destination at 11
        ///
        /// When checking the profile "take the bus at 9:59, arrive at 11:15", this profile will (correctly) be refused as suboptimal
        /// However, this means that the profile from A - arriving at 9:59 will _refuse_ to transfer (too little time)
        /// and will result in a journey: Leave A at 8:30, transfer at B tot the train of 10:00 to arrive at 11:00...
        ///
        /// ... While we just could have taken the bus at 9:30 and arrived at 11:15. 45 minutes and one transfer less (and probably cheaper as well)
        /// 
        ///
        /// (Note that I thought it was not needed at first and all could be modelled with just the station journeys.
        /// I've spent a few days figuring out why certain routes where omitted)
        /// </summary>
        private readonly Dictionary<string, ParetoFrontier<T>> _tripJourneys =
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
            : this(new List<Uri> {departureLocation}, new List<Uri> {targetLocation}, earliestDeparture, lastArrival,
                profile)
        {
            if (targetLocation.Equals(departureLocation))
            {
                throw new ArgumentException("Departure and target location are the same");
            }
        }

        /// <inheritdoc />
        public ProfiledConnectionScan(IEnumerable<Uri> departureLocations,
            IEnumerable<Uri> targetLocations,
            DateTime earliestDeparture, DateTime lastArrival,
            Profile<T> profile) : this
        (MapList(departureLocations, earliestDeparture), MapList(targetLocations, lastArrival),
            earliestDeparture, lastArrival, profile)
        {
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

            if (earliestDeparture >= lastArrival)
            {
                throw new ArgumentException(
                    "Departure time falls after arrival time. Do you intend to travel backwards in time? If so, lend me that time machine!");
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
            _profile = profile.MemoizingPathsProfile();
            _knownLocations = new ActiveLocationTracker<T>(earliestDeparture, profile);
            _empty = new ParetoFrontier<T>(_profileComparator);
        }

        /// <summary>
        /// Calculate possible journeys from the given provider,
        /// where the Uri points to the timetable of the last allowed arrival at the destination station
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, IEnumerable<Journey<T>>> CalculateJourneys()
        {
            var tt = _profile.ConnectionsProvider.GetTimeTable(_lastArrival);
            IConnection c = null;
            do
            {
                tt = new ValidatingTimeTable(_profile, tt);
                var cons = tt.ConnectionsReversed();
                foreach (var conn in cons)
                {
                    c = conn;
                    // The list is sorted by departure time
                    // The algorithm starts with the latest departure time and goes earlier


                    if (c.DepartureTime() < _earliestDeparture)
                    {
                        break;
                    }

                    AddConnection(c);
                }

                tt = _connectionsProvider.GetTimeTable(tt.PreviousTable());
            } while (c != null && c.DepartureTime() >= _earliestDeparture && tt != null);


            // Post processing
            // Prepare a neat dictionary for each departure location of the end user
            var result = new Dictionary<string, ParetoFrontier<T>>();
            // Note that we should still append the end walks
            foreach (var depStop in _footpathsIn.Keys)
            {
                if (!_stationJourneys.ContainsKey(depStop))
                {
                    continue;
                }

                var walk = _footpathsIn[depStop];
                var actualDeparture = walk.DepartureLocation();
                var journeys = _stationJourneys[depStop].Frontier;


                if (!result.ContainsKey(actualDeparture.ToString()))
                {
                    result[actualDeparture.ToString()] = new ParetoFrontier<T>(_profileComparator);
                }


                foreach (var journey in journeys)
                {
                    if (walk.DepartureLocation() == walk.ArrivalLocation())
                    {
                        result[actualDeparture.ToString()].AddToFrontier(journey.Reverse());
                        continue;
                    }

                    var timedWalk = walk.MoveArrivalTime(journey.Connection.DepartureTime());
                    if (timedWalk.DepartureTime() < _earliestDeparture)
                    {
                        continue;
                    }

                    var chained = new Journey<T>(journey, timedWalk);
                    chained = chained.Reverse();
                    result[actualDeparture.ToString()].AddToFrontier(chained);
                }
            }

            var resultIEnum = new Dictionary<string, IEnumerable<Journey<T>>>();
            foreach (var key in result.Keys)
            {
                resultIEnum[key] = result[key].Frontier;
            }

            return resultIEnum;
        }

        /// <summary>
        /// Handles a connection backwards in time
        /// </summary>
        private void AddConnection(IConnection c)
        {
            // 0) We extend the known trip journeys of this connection
            ExtendTrip(c);


            // 1) First, handle outgoing connections (i.e; footpaths out).
            // They provide the first entries in _stationJourneys
            if (_footpathsOut.ContainsKey(c.ArrivalLocation().ToString()))
            {
                // We can arrive in one of our target locations.
                var walk = _footpathsOut[c.ArrivalLocation().ToString()];
                if (walk.DepartureLocation().Equals(walk.ArrivalLocation()))
                {
                    // NO target walks; we are at one of our destinations
                    // We can add this connection 'as is' to the station- & tripJourneys (after which we are done with this connection)
                    var journey = new Journey<T>(
                        _profile.StatsFactory.InitialStats(c), c);
                    ConsiderJourney(journey);
                    ConsiderTripJourney(journey);
                    _knownLocations.AddKnownLocation(c.DepartureLocation());
                    return;
                }

                // We arrive in a stop, from which we can walk to our target
                // This implies that there is a 'walking connection'
                // which leaves at the arrival time of C
                walk = walk.MoveDepartureTime(c.ArrivalTime());
                if (walk.ArrivalTime() <= _lastArrival)
                {
                    var journey = new Journey<T>(_profile.StatsFactory.InitialStats(walk), walk);

                    ConsiderJourney(journey);
                    ConsiderTripJourney(journey);
                    _knownLocations.AddKnownLocation(c.DepartureLocation());
                    // The connection itself: we leave that one to be handled by the rest of the code
                }

                // We still handle the connection as the rest of the journey
                // // Perhaps the bus will continue to drive us closer to the station
            }

            // Could this connection be usable via a footpath?
            // Get all the walks from the arrival to nearby stops
            // If we can walk to a nearby journey, this implies we can get out
            var walks = _knownLocations.WalksFrom(c.ArrivalLocation());


            // 2) Can the connection be used? If not, skip
            if (!_stationJourneys.ContainsKey(c.ArrivalLocation().ToString()) && walks == null)
            {
                // NO way out of the arrival station yet
                // If there is no way out of the arrival station yet, this implies that the trip won't be in trip journeys either
                return;
            }


            // 3) Could this connection be reachable by foot, from the departure location?
            // We handle those in the postprocessing

            // All the edge (literally) cases are handled now
            // ---------------------------------------------------------------------
            // Time for the core algorithm

            // Get the journeys to our target, with conn.Arrival as departure Location
            // We will multiply all those connections with the current connection
            var journeysToEnd = GetStationJourney(c.ArrivalLocation(), _empty);


            // The flag 'journeyAdded' be true if this connection is taken in at least one journey
            // If the connection is taken at least once, we should calculate the walking connections into the departure station
            bool journeyAdded = false; // We arrive in a stop, from which we can walk to our target


            // This flag indicates that we encountered this trip for the first time
            // This means that we "cannot stay seated" in this trip and have to leave at the arrival station of the connection
            // The optimal trip journeys is thus be the resulting pareto front that we already have
            bool firstTripEncounter = c.Trip() != null && !_tripJourneys.ContainsKey(c.Trip().ToString());

            foreach (var j in journeysToEnd.Frontier)
            {
                if (c.ArrivalTime() > j.Connection.DepartureTime())
                {
                    // We missed this journey to the target
                    // Note that the journeysToEnd are ordered from last to earliest departure
                    // If we miss the journey j, the next will depart even earlier
                    // So we can break the loop
                    break;
                }


                // Consider journey might also create a Transfer if needed
                journeyAdded |= ConsiderCombination(c, j, firstTripEncounter);
            }

            if (!firstTripEncounter && c.Trip() != null)
            {
                // We can also not leave the current trip
                // Perhaps that will take us to our destination faster then a potential transfer?
                var key = c.Trip().ToString();
                var trips = _tripJourneys[key].Frontier;
                foreach (var j in trips)
                {
                    // Connection 'c' is already included in our trips
                    journeyAdded |= ConsiderJourney(j);
                }
            }

            if (walks != null)
            {
                // We try each walk
                foreach (var walk in walks)
                {
                    var remoteStation = walk.ArrivalLocation();
                    if (remoteStation == c.DepartureLocation())
                    {
                        continue;
                    }

                    var remoteJourneys = GetStationJourney(remoteStation, _empty).Frontier;
                    foreach (var journey in remoteJourneys)
                    {
                        var timedWalk = walk.MoveDepartureTime(c.ArrivalTime());
                        if (timedWalk.ArrivalTime() >= journey.Connection.DepartureTime())
                        {
                            // we can't make the transfer
                            continue;
                        }

                        var chained =
                            new Journey<T>(new Journey<T>(
                                    journey,
                                    timedWalk),
                                c);
                        journeyAdded |= ConsiderJourney(chained);
                    }
                }
            }

            if (journeyAdded)
            {
                // The connection was taken in at least one journey.
                // This means that each walk from a close by stop into this connection
                // should be considered as well
                // ConsiderInterstopWalks(c);
                _knownLocations.AddKnownLocation(c.DepartureLocation());
            }
        }


        /// <summary>
        /// Combines the connection and previous journey; after which the resulting journey might be added into tripJourney, stationJourney or both
        /// </summary>
        private bool ConsiderCombination(IConnection c, Journey<T> journey, bool considerTripJourney)
        {
            // Chaining to the start of the journey, not the end -> We keep track of the departure time (although the time is not actually used)
            var chained = new Journey<T>(journey, c);
            // We keep track if we actually use this connection
            bool added;
            // Lets add the journey to the frontier - if needed
            added = ConsiderJourney(chained);

            if (considerTripJourney)
            {
                added |= ConsiderTripJourney(chained);
            }

            return added;
        }

        private void ExtendTrip(IConnection c)
        {
            if (c.Trip() == null)
            {
                return;
            }

            var key = c.Trip().ToString();
            if (!_tripJourneys.ContainsKey(key))
            {
                return;
            }

            var frontier = _tripJourneys[key].Frontier;
            for (var i = 0; i < frontier.Count; i++)
            {
                frontier[i] = new Journey<T>(frontier[i], c);
            }
        }

        /// <summary>
        /// Considers the given journey for the trip.
        /// </summary>
        /// <param name="considered"></param>
        /// <returns></returns>
        private bool ConsiderTripJourney(Journey<T> considered)
        {
            var trip = (considered.Connection as IConnection)?.Trip()?.ToString();
            if (trip == null)
            {
                return false;
            }

            if (!_tripJourneys.ContainsKey(trip))
            {
                var frontier = new ParetoFrontier<T>(_profileComparator);
                frontier.AddToFrontier(considered);
                _tripJourneys[trip] = frontier;
                return true;
            }

            return _tripJourneys[trip].AddToFrontier(considered);
        }

        ///   <summary>
        ///   This method is called when a new startStation can be reached with the given Journey `considered`.
        ///  
        ///   The method will consider if this journey is profile optimal and should thus be included.
        ///   If it is not, it will be ignored.
        ///
        /// Returns True if the journey was effectively added
        ///   </summary>
        /// <param name="considered"></param>
        private bool ConsiderJourney(Journey<T> considered)
        {
            var startStation = considered.Connection.DepartureLocation().ToString();
            if (!_stationJourneys.ContainsKey(startStation))
            {
                // This is the first journey that takes us from this 'start stations' 
                // towards our destination. We add it anyways, after which we are done
                _stationJourneys[startStation] = new ParetoFrontier<T>(_profileComparator);
            }

            // We only add the journey to the list if it is not dominated
            // Because the connections are scanned in decreasing departure time, there cannot be a connection with an earlier departure time
            // It is therefore sufficient for the domination test to look at the earliest (in time = last in array) pairs already in the array.

            // As
            // - The list of stationJourneys has the soonest departures on the back
            // - Connections are handled from later to earlier departure time
            // The _last_ elements in the list will have the _earliest_ departure times.
            //
            // Because a profiled stats comparator will always compare the start time of the journey,
            // we _only_ have to do a domination check against the last few elements IFF the departure time is the same 
            // TODO Implement this optimalization, in ParetoFrontier for example
            return _stationJourneys[startStation].AddToFrontier(considered);
        }


        /// <summary>
        /// Converts the list into a list of genesis connections.
        /// Used in the constructor; small helper function
        /// </summary>
        private static IEnumerable<WalkingConnection> MapList(IEnumerable<Uri> locations, DateTime time)
        {
            var l = new List<WalkingConnection>();
            foreach (var uri in locations)
            {
                l.Add(new WalkingConnection(uri, time));
            }

            return l;
        }

        private ParetoFrontier<T> GetStationJourney(Uri key, ParetoFrontier<T> value)
        {
            return GetStationJourney(key.ToString(), value);
        }

        private ParetoFrontier<T> GetStationJourney(string key, ParetoFrontier<T> value)
        {
            return _stationJourneys.ContainsKey(key) ? _stationJourneys[key] : value;
        }
    }
}