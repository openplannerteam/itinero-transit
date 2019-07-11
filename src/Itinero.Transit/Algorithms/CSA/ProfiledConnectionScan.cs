using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Filter;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Algorithms.CSA
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
    internal class ProfiledConnectionScan<T> where T : IJourneyMetric<T>
    {
        internal readonly IConnectionEnumerator _connections;
        private readonly IStopsReader _stopsReader;
        internal readonly ulong _earliestDeparture, _lastArrival;
        internal readonly HashSet<StopId> _departureLocations;

        internal readonly HashSet<ProfiledParetoFrontier<T>> _departureFrontiers =
            new HashSet<ProfiledParetoFrontier<T>>();

        internal readonly HashSet<IStop> _targetLocations;
        internal readonly HashSet<StopId> _targetLocationsIds;

        private readonly MetricComparator<T> _comparator;

        private readonly T _metricFactory;

        // Indicates if connections can not be taken due to external reasons (e.g. earlier scan)
        private readonly IConnectionFilter _filter;
        private readonly IJourneyFilter<T> _journeyFilter;
        private readonly IMetricGuesser<T> _guesser;

        /// <summary>
        /// Rules how much penalty is given to go from one connection to another, without changing stations
        /// </summary>
        internal readonly IOtherModeGenerator _transferPolicy;

        /// <summary>
        /// Rules how much time is needed to walk from one stop to another.
        /// This IOtherModeGenerator may show complex behaviour, e.g. based on what the first or last stop is for first- and lastmile policies
        /// </summary>
        internal readonly IOtherModeGenerator _walkPolicy;

        // Placeholder empty frontier; used when a frontier is needed but not present.
        private readonly ProfiledParetoFrontier<T> _empty;

        /// <summary>
        /// Maps each stop onto a pareto-frontier.
        ///
        /// If the traveller were to appear in a given station, he could lookup here
        /// which non-dominated trips he could take to his destination - including time needed to get on the train
        /// <br />
        /// If the station isn't in the dictionary yet, this means no trip from this station has been already found.
        ///
        /// Also known as 'S' in the paper
        ///
        /// </summary>
        private readonly Dictionary<StopId, ProfiledParetoFrontier<T>> _stationJourneys =
            new Dictionary<StopId, ProfiledParetoFrontier<T>>();


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
        private readonly Dictionary<TripId, ProfiledParetoFrontier<T>> _tripJourneys =
            new Dictionary<TripId, ProfiledParetoFrontier<T>>();


        ///  <summary>
        ///  Create a new ProfiledConnectionScan algorithm.
        ///  </summary>
        public ProfiledConnectionScan(ScanSettings<T> settings)
        {
            _comparator = settings.Profile.ProfileComparator;
            _journeyFilter = settings.Profile.JourneyFilter;

            _targetLocations = new HashSet<IStop>();
            _targetLocationsIds = new HashSet<StopId>();
            _stopsReader = settings.StopsReader;
            foreach (var (target, journey) in settings.TargetStop)
            {
                if (journey != null)
                {
                    throw new ArgumentException("PCS does not support target location journeys.");
                }

                _stopsReader.MoveTo(target);
                _targetLocations.Add(new Stop(_stopsReader));
                _targetLocationsIds.Add(target);
            }

            _departureLocations = new HashSet<StopId>();
            foreach (var (target, journey) in settings.DepartureStop)
            {
                if (journey != null)
                {
                    throw new ArgumentException("PCS does not support departure location journeys.");
                }

                _departureLocations.Add(target);
                // We already create a frontier for each of the destinations...
                var frontier = new ProfiledParetoFrontier<T>(_comparator, _journeyFilter);
                _stationJourneys[target] = frontier;
                // ...and keep track of them. This index server to compare with the metricGuessers
                _departureFrontiers.Add(frontier);
            }

            if (settings.ExampleJourney != null)
            {
                var example = settings.ExampleJourney;
                _stationJourneys[example.Location].AddToFrontier(example);
            }

            _earliestDeparture = settings.EarliestDeparture.ToUnixTime();
            _lastArrival = settings.LastArrival.ToUnixTime();

            _connections = settings.ConnectionsEnumerator;

             _empty = new ProfiledParetoFrontier<T>(_comparator, _journeyFilter);
            _metricFactory = settings.Profile.MetricFactory;
            _transferPolicy = settings.Profile.InternalTransferGenerator;
            _walkPolicy = settings.Profile.WalksGenerator;

            _guesser = settings.MetricGuesser;

            // Apply the connection filter from the profile
            _filter = settings.Profile.ConnectionFilter;
            _filter?.CheckWindow(_earliestDeparture, _lastArrival);


            // The isochronefilter is created by a preceding EAS or isochrone scan and speeds up by a critical speed....
            // But it requires some special attention
            var isochroneFilter = settings.Filter;

            if (isochroneFilter != null && isochroneFilter.ValidWindow(_earliestDeparture, _lastArrival))
            {
                // There is an isochrone filter and the timing does work out - hooray!
                // But - the isochronefilter is often too strict
                // (see https://github.com/openplannerteam/itinero-transit/issues/63)
                // For this, a few cases are whitelisted, namely:
                // - If the connection arrives at a destination or departs at a departure stop
                // - If we are on the trip already
                // These are handled by the SpecialCaseFilter

                HashSet<StopId> whiteList = new HashSet<StopId>();
                whiteList.UnionWith(_departureLocations);
                whiteList.UnionWith(_targetLocationsIds);

                var filter = new SpecialCaseConnectionFilter<T>(
                    isochroneFilter,
                    whiteList,
                    _tripJourneys
                );
                // Install the extra filter
                _filter = ConnectionFilterAggregator.CreateFrom(filter, _filter);
            }
        }


        public List<Journey<T>> CalculateJourneys()
        {
            var enumerator = _connections;
            // Move the enumerator after the last arrival time
            enumerator.MoveTo(_lastArrival);

            var c = new Connection();
            while (
                enumerator.HasPrevious() &&
                enumerator.CurrentDateTime >= _earliestDeparture)
            {
                enumerator.Current(c);
                IntegrateConnection(c);
            }

            // The main algorithm is done
            // Time to gather the results
            // To do this, we collect all journeys from every possible departure location
            var revJourneys = new List<Journey<T>>();
            foreach (var location in _departureLocations)
            {
                if (!_stationJourneys.ContainsKey(location))
                {
                    // Seemed it wasn't possible to reach the destination from this departure location
                    continue;
                }

                var journeys = _stationJourneys[location].Frontier;

                foreach (var j in journeys)
                {
                    j.ReverseAndAddTo(revJourneys); // Reverse, add to revJourneys
                }
            }

            if (!revJourneys.Any())
            {
                // Nothing found
                return null;
            }


            // Sort journeys by absolute departure time
            var sorted = revJourneys.OrderBy(journey => journey.Root.Time).ToList();

            return sorted;
        }


        /// <summary>
        /// Looks to this single connection, the actual PCS step
        /// </summary>
        /// <param name="c"></param>
        private void IntegrateConnection(Connection c)
        {
            if (c.ArrivalTime > _lastArrival)
            {
                return;
            }

            if (_filter != null
                && !_filter.CanBeTaken(c)
                && !_tripJourneys.ContainsKey(c.TripId)
            )
            {
                // Why all those conditions? See https://github.com/openplannerteam/itinero-transit/issues/63
                return;
            }


            /*What if we went by foot after taking C?*/
            var journeyT1 = WalkToTargetFrom(c);

            /*What are the optimal journeys when remaining seated on this connection?*/
            var journeyT2 = ExtendTrip(c);

            /*What if we transfer in this station?
             */
            var journeyT3 = TransferAfter(c);

            /* Lets pick out the best journeys that have C in them*/
            var journeys = PickBestJourneys(journeyT1, journeyT2, journeyT3);

            if (journeys.Frontier.Count == 0)
            {
                // We can't take this connection to the destination in the first place
                return;
            }

            /*
             We have handled this connection and have a few journeys containing C taking us to a destination.
             This means we can update the various tables for the rest of the algo.
             
             The first to update is the trip table, where we add the best option.
             Again, we don't check whether we can get on or off
             */
            _tripJourneys[c.TripId] = journeys;


            // Now, we update the journeys from the departure station to the arrival station
            // However, we can only do this if we _can get on_ the connection
            if (!c.CanGetOn())
            {
                return;
            }

            // And ofc, we have a pretty good way out from the departure stop as well
            if (!_stationJourneys.ContainsKey(c.DepartureStop))
            {
                _stationJourneys[c.DepartureStop] = new ProfiledParetoFrontier<T>(_comparator, _journeyFilter);
            }

            var addedJourneys = _stationJourneys[c.DepartureStop].AddAllToFrontier(journeys.Frontier);


            /*We can depart at c.DepartureStop quite optimally.
             This means that we can walk from other locations to here to take an optimal journey to the destination
             UpdateFootpaths calculates all those walks and adds them to the _stationJourneys.
             (Note that all 'addedJourneys' have location == 'c.DepartureStop'
             */
            UpdateFootpaths(addedJourneys, c.DepartureStop);
        }


        /// <summary>
        /// Gives the journeys that would result if we were to leave C and walk
        /// to the destination
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private Journey<T> WalkToTargetFrom(Connection c)
        {
            if (!c.CanGetOff())
            {
                // We can't get off c and thus can't walk to the destination
                return Journey<T>.InfiniteJourney;
            }

            if (_targetLocationsIds.Contains(c.DepartureStop))
            {
                // No use to depart, is it?
                return Journey<T>.InfiniteJourney;
            }

            if (_targetLocationsIds.Contains(c.ArrivalStop))
            {
                // We already are at a possible target location

                // We create a genesis journey...
                var arrivingJourney = new Journey<T>
                (c.ArrivalStop, c.ArrivalTime, _metricFactory.Zero(),
                    Journey<T>.ProfiledScanJourney);
                // ... and put 'C' before it
                var journey = arrivingJourney.ChainBackward(c);
                return journey;
            }


            // Let's calculate the various times to walk towards each possible destination
            _stopsReader.MoveTo(c.ArrivalStop);
            var walkingTimes =
                _walkPolicy?.TimesBetween( /* IStop from */ _stopsReader, _targetLocations);
            if (walkingTimes == null || walkingTimes.Count == 0)
            {
                return null;
            }

            // Ofc, we only care about the fastest arrival:

            StopId? fastestTarget = null;
            var fastestTime = uint.MaxValue;
            foreach (var kvpair in walkingTimes)
            {
                var targetLoc = kvpair.Key;
                var timeNeeded = kvpair.Value;
                if (timeNeeded < fastestTime)
                {
                    fastestTime = timeNeeded;
                    fastestTarget = targetLoc;
                }
            }

            if (fastestTarget == null)
            {
                return null;
            }

            return
                // The 'genesis' indicating when we arrive ... 
                new Journey<T>
                    (fastestTarget.Value, c.ArrivalTime + fastestTime,
                        _metricFactory.Zero(),
                        Journey<T>.ProfiledScanJourney)
                    // ... the walking part ...
                    .ChainSpecial
                        (Journey<T>.OTHERMODE, c.ArrivalTime, c.ArrivalStop, new TripId(_walkPolicy))
                    // ... the connection part ...
                    .ChainBackward(c)
                ;
        }

        /// <summary>
        /// Chains the given connection to the needed trips
        /// </summary>
        /// <param name="c"></param>
        private ProfiledParetoFrontier<T> ExtendTrip(Connection c)
        {
            var key = c.TripId;
            if (!_tripJourneys.ContainsKey(key))
            {
                return _empty;
            }

            // If we already are on the trip, we can stay seated. We don't have to check if we can get on or off

            var pareto = _tripJourneys[key];
            var frontier = pareto.Frontier;
            for (var i = 0; i < frontier.Count; i++)
            {
                if (frontier[i].Root.Location.Equals(c.DepartureStop))
                {
                    // We are walking in circles
                    frontier.RemoveAt(i);
                    i--;
                    continue;
                }

                frontier[i] = frontier[i].ChainBackward(c);
            }

            return pareto;
        }


        private ProfiledParetoFrontier<T> TransferAfter(Connection c)
        {
            // We have just taken C and are gonna transfer
            // In what possible journeys (if any) to the destination will this result?


            // Is it in the first place possible to make a meaningful transfer?
            if (!_stationJourneys.ContainsKey(c.ArrivalStop))
            {
                // No! Once we transfer out of this station, we are stuck there
                return _empty;
            }

            if (!c.CanGetOff())
            {
                // No! We can not get out of this connection;
                return _empty;
            }

            // We get all possible, pareto optimal journeys departing here...
            var pareto = _stationJourneys[c.ArrivalStop];
            // ... we try to clean up this frontier a little ...
            pareto.Clean(_guesser, _departureFrontiers);

            // .. and we extend them with c. What is non-dominated, we return
            return pareto.ExtendFrontierBackwards(_stopsReader, c, _transferPolicy);
        }


        /// <summary>
        /// When a new journey to the destination is discovered departing at A at time T,
        /// the destination can be reached too by walking towards A (a few minutes earlier) and then taking that journey.
        ///
        ///  This method calculates all those journeys walking towards A from nearby stops
        ///
        /// Note that every journey should have 'Location' to be equal 'cDepartureLocation'
        /// </summary>
        private void UpdateFootpaths(IEnumerable<Journey<T>> journeys, StopId cDepartureStop)
        {
            if (_walkPolicy == null || _walkPolicy.Range() <= 0f)
            {
                return;
            }

            var withWalks = journeys.WalkTowards(cDepartureStop, _walkPolicy, _stopsReader);


            foreach (var j in withWalks)
            {
                // The journey starts at this location now at a slightly earlier time
                var stopId = j.Location;

                if (stopId.Equals(j.Root.Location))
                {
                    // We are walking in circles... 
                    continue;
                }


                // And add this journey with walk to the pareto frontier
                if (!_stationJourneys.ContainsKey(stopId))
                {
                    _stationJourneys[stopId] = new ProfiledParetoFrontier<T>(_comparator, _journeyFilter);
                }

                _stationJourneys[stopId].AddToFrontier(j);
            }
        }


        private ProfiledParetoFrontier<T> PickBestJourneys(Journey<T> j, ProfiledParetoFrontier<T> a,
            ProfiledParetoFrontier<T> b)
        {
            if (a.Frontier.Count == 0 && b.Frontier.Count == 0)
            {
                if (ReferenceEquals(j, Journey<T>.InfiniteJourney))
                {
                    return _empty;
                }

                var front = new ProfiledParetoFrontier<T>(_comparator, _journeyFilter);
                front.AddToFrontier(j);
                return front;
            }

            var frontier = ParetoExtensions.Combine(a, b);
            frontier.AddToFrontier(j);

            return frontier;
        }

        public Dictionary<StopId, List<Journey<T>>> Isochrone()
        {
            var isochrone = new Dictionary<StopId, List<Journey<T>>>();

            foreach (var option in _stationJourneys)
            {
                var location = option.Key;
                var frontier = option.Value;

                var journeys = new List<Journey<T>>();

                foreach (var j in frontier.Frontier)
                {
                    j.ReverseAndAddTo(journeys);
                }

                isochrone.Add(location, journeys);
            }

            return isochrone;
        }

        /// <summary>
        /// Gives raw access to the internal data structure of PCS.
        /// This is mostly meant for debugging
        /// </summary>
        /// <returns></returns>
        public Dictionary<StopId, ProfiledParetoFrontier<T>> StationJourneys()
        {
            return _stationJourneys;
        }
    }

    internal class SpecialCaseConnectionFilter<T> : IConnectionFilter where T : IJourneyMetric<T>
    {
        private readonly IConnectionFilter _implementation;
        private readonly HashSet<StopId> _whiteListed;
        private readonly Dictionary<TripId, ProfiledParetoFrontier<T>> _tripJourneys;

        /// <summary>
        /// Creates a special case filter.
        /// This filter is meant to be used by PCS.
        /// </summary>
        /// <param name="implementation"></param>
        /// <param name="whiteListed"></param>
        /// <param name="tripJourneys">A POINTER to the dictionary containing the trips. </param>
        public SpecialCaseConnectionFilter(IConnectionFilter implementation,
            HashSet<StopId> whiteListed,
            Dictionary<TripId, ProfiledParetoFrontier<T>> tripJourneys
        )
        {
            _implementation = implementation;
            _whiteListed = whiteListed;
            _tripJourneys = tripJourneys; // IMPORTANT: this is a pointer to the datastructure used in PCS!
        }

        public bool CanBeTaken(Connection c)
        {
            if (_whiteListed.Contains(c.DepartureStop) ||
                _whiteListed.Contains(c.ArrivalStop) ||
                _tripJourneys.ContainsKey(c.TripId)
            )
            {
                return true;
            }

            return _implementation.CanBeTaken(c);
        }

        public void CheckWindow(ulong depTime, ulong arrTime)
        {
            _implementation.CheckWindow(depTime, arrTime);
        }
    }
}