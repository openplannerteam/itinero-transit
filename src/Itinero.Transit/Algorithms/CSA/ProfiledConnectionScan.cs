using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Filter;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Algorithms.CSA
{
    using UnixTime = UInt64;


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
        private readonly IConnectionEnumerator _connections;
        private readonly IStopsReader _stopsReader;
        private readonly UnixTime _earliestDeparture, _lastArrival;
        private readonly List<LocationId> _departureLocations;
        private readonly List<LocationId> _targetLocations;

        private readonly ProfiledMetricComparator<T> _comparator;

        private readonly T _metricFactory;

        // Indicates if connections can not be taken due to external reasons (e.g. earlier scan)
        private readonly IConnectionFilter _filter;

        /// <summary>
        /// Rules how much penalty is given to go from one connection to another, without changing stations
        /// </summary>
        private readonly IOtherModeGenerator _transferPolicy;


        /// <summary>
        /// Rules how the traveller walks from one stop to another
        /// </summary>
        private readonly IOtherModeGenerator _walkPolicy;

        // Placeholder empty frontier; used when a frontier is needed but not present.
        private readonly ParetoFrontier<T> _empty;

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
        private readonly Dictionary<LocationId, ParetoFrontier<T>> _stationJourneys =
            new Dictionary<LocationId, ParetoFrontier<T>>();


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
        private readonly Dictionary<TripId, ParetoFrontier<T>> _tripJourneys =
            new Dictionary<TripId, ParetoFrontier<T>>();


        ///  <summary>
        ///  Create a new ProfiledConnectionScan algorithm.
        ///  </summary>
        public ProfiledConnectionScan(ScanSettings<T> settings)
        {
            _targetLocations = new List<LocationId>();
            foreach (var (target, journey) in settings.TargetStop)
            {
                if (journey != null)
                {
                    throw new ArgumentException("PCS does not support target location journeys.");
                }

                _targetLocations.Add(target);
            }

            _departureLocations = new List<LocationId>();
            foreach (var (target, journey) in settings.DepartureStop)
            {
                if (journey != null)
                {
                    throw new ArgumentException("PCS does not support departure location journeys.");
                }

                _departureLocations.Add(target);
            }


            _earliestDeparture = settings.EarliestDeparture.ToUnixTime();
            _lastArrival = settings.LastArrival.ToUnixTime();

            _connections = settings.ConnectionsEnumerator;
            _stopsReader = settings.StopsReader;

            _comparator = settings.Comparator;
            _empty = new ParetoFrontier<T>(_comparator);
            _metricFactory = settings.MetricFactory;
            _transferPolicy = settings.TransferPolicy;
            _walkPolicy = settings.WalkPolicy;
            _filter = settings.Filter;
            _filter?.CheckWindow(_earliestDeparture, _lastArrival);
        }


        public List<Journey<T>> CalculateJourneys()
        {
            var enumerator = _connections;
            // Move the enumerator after the last arrival time
            enumerator.MovePrevious(_lastArrival);

            while (enumerator.DepartureTime >= _earliestDeparture)
            {
                if (!IntegrateBatch(enumerator))
                {
                    // Database depleted
                    break;
                }
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
        /// Integrates all connections of the enumerator where the departure time is the current departure time
        /// </summary>
        /// <param name="enumerator"></param>
        private bool IntegrateBatch(IConnectionEnumerator enumerator)
        {
            var depTime = enumerator.DepartureTime;
            do
            {
                IntegrateConnection(enumerator);
                if (!enumerator.MovePrevious())
                {
                    return false;
                }
            } while (depTime == enumerator.DepartureTime);

            return true;
        }


        /// <summary>
        /// Looks to this single connection, the actual PCS step
        /// </summary>
        /// <param name="c"></param>
        private void IntegrateConnection(IConnection c)
        {
            if (c.ArrivalTime > _lastArrival)
            {
                return;
            }

            if (_filter != null && !_filter.CanBeTaken(c))
            {
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
             We have handled this connection and have a few journeys containing C
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
                _stationJourneys[c.DepartureStop] = new ParetoFrontier<T>(_comparator);
            }

            _stationJourneys[c.DepartureStop].AddAllToFrontier(journeys.Frontier);


            /*We can depart at c.DepartureStop quite optimally.
             This means that we can walk from other locations to here to take an optimal journey to the destination
             UpdateFootpaths calculates all those walks and adds them to the _stationJourneys
             */
            UpdateFootpaths(journeys, c.DepartureStop);
        }


        /// <summary>
        /// Gives the journeys that would result if we were to leave C and walk
        /// to the destination
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private Journey<T> WalkToTargetFrom(IConnection c)
        {
            if (!c.CanGetOff())
            {
                // We can't get off c and thus can't walk to the destination
                return Journey<T>.InfiniteJourney;
            }


            Journey<T> journeyWithWalk = null;

            foreach (var targetLocation in _targetLocations)
            {
                if (Equals(c.ArrivalStop, targetLocation))
                {
                    // We are at a possible target location
                    // No real need to walk
                    var arrivingJourney = new Journey<T>
                    (targetLocation, c.ArrivalTime, _metricFactory.Zero(),
                        Journey<T>.ProfiledScanJourney);
                    var journey = arrivingJourney.ChainBackward(c);
                    return journey;
                }

                // We walk from the connection to the target location...

                if (_walkPolicy == null)
                {
                    // We are not able to walk - no such policy given
                    continue;
                }

                // The journey which walks towards the stop
                // We start by calculating the time needed

                var timeNeeded = _walkPolicy.TimeBetween(_stopsReader, c.ArrivalStop, targetLocation);

                if (uint.MaxValue == timeNeeded)
                {
                    continue;
                }
                // We can walk to the target destination!
                // When would be arriving...?
                var arrivalTime = c.ArrivalTime + timeNeeded;
                
                // And more importantly, is it faster then an earlier found walking journey?

                if (journeyWithWalk != null && journeyWithWalk.Time < arrivalTime)
                {
                    // NO! The already found journey with walk is faster
                    continue;
                }

                var genesisEnd = new Journey<T>
                (targetLocation,arrivalTime,
                    _metricFactory.Zero(),
                    Journey<T>.ProfiledScanJourney);


                var journeyWithWalkOnly = _walkPolicy.CreateArrivingTransfer(
                    _stopsReader,
                    genesisEnd,
                    c.ArrivalStop);

                if (journeyWithWalkOnly == null)
                {
                    continue;
                }
                journeyWithWalk = journeyWithWalkOnly.ChainBackward(c);

            }

            return journeyWithWalk;
        }

        /// <summary>
        /// Chains the given connection to the needed trips
        /// </summary>
        /// <param name="c"></param>
        private ParetoFrontier<T> ExtendTrip(IConnection c)
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
                frontier[i] = frontier[i].ChainBackward(c);
            }

            return pareto;
        }


        private ParetoFrontier<T> TransferAfter(IConnection c)
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
            // .. and we extend them with c. What is non-dominated, we return
            return pareto.ExtendFrontierBackwards(_stopsReader, c, _transferPolicy);
        }


        /// <summary>
        /// When a departure stop can be reached by a new journey, each close by stop can be reached via walking too
        /// This method creates all those footpaths and transfers 
        /// </summary>
        /// <param name="journeys"></param>
        /// <param name="cDepartureStop"></param>
        private void UpdateFootpaths(ParetoFrontier<T> journeys, LocationId cDepartureStop)
        {
            if (_walkPolicy.Range() <= 0f)
            {
                return;
            }

            _stopsReader.MoveTo(cDepartureStop);
            var nearbyStops = _stopsReader.LocationsInRange
                (_stopsReader.Latitude, _stopsReader.Longitude, _walkPolicy.Range());


            foreach (var possibleStartingStop in nearbyStops)
            {
                var stopId = possibleStartingStop.Id;
                if (stopId.Equals(cDepartureStop))
                {
                    continue;
                }

                foreach (var journey in journeys.Frontier)
                {
                    // Create a walk from 'possibleStartingStop' to 'cDepartureStop', where an optimal journey is taken to the destination
                    var journeyWithWalk = _walkPolicy.CreateArrivingTransfer(
                        _stopsReader, journey, stopId);

                    // And add this journey with walk to the pareto frontier
                    if (!_stationJourneys.ContainsKey(stopId))
                    {
                        _stationJourneys[stopId] = new ParetoFrontier<T>(_comparator);
                    }

                    _stationJourneys[stopId].AddToFrontier(journeyWithWalk);
                }
            }
        }


        private ParetoFrontier<T> PickBestJourneys(Journey<T> j, ParetoFrontier<T> a, ParetoFrontier<T> b)
        {
            if (a.Frontier.Count == 0 && b.Frontier.Count == 0)
            {
                if (ReferenceEquals(j, Journey<T>.InfiniteJourney))
                {
                    return _empty;
                }

                var front = new ParetoFrontier<T>(_comparator);
                front.AddToFrontier(j);
                return front;
            }

            var frontier = ParetoExtensions.Combine(a, b);
            frontier.AddToFrontier(j);

            return frontier;
        }

        public Dictionary<LocationId, List<Journey<T>>> Isochrone()
        {
            var isochrone = new Dictionary<LocationId, List<Journey<T>>>();

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
    }
}