using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

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
    public class ProfiledConnectionScan<T> where T : IJourneyStats<T>
    {
        private readonly TransitDb.TransitDbSnapShot _tdb;
        private readonly ConnectionsDb _connectionsProvider;
        private readonly UnixTime _earliestDeparture, _lastArrival;
        private readonly List<(uint, uint)> _departureLocations;
        private readonly List<(uint, uint)> _targetLocations;

        private readonly ProfiledStatsComparator<T> _comparator;

        private readonly T _statsFactory;

        // Indicates if connections can not be taken due to external reasons (e.g. earlier scan)
        private readonly IConnectionFilter _filter;

        private readonly Journey<T> _possibleJourney;

        /// <summary>
        /// Rules how much penalty is given to go from one connection to another
        /// </summary>
        private readonly IOtherModeGenerator _transferPolicy;

        // Placeholder empty frontier; used when a frontier is needed but not present.
        private readonly ParetoFrontier<T> _empty;

        /// <summary>
        /// Maps each stop onto a pareto-frontier.
        ///
        /// If the traveller were to appear in a given station, he could lookup here
        /// which non-dominated trips he could take to his destination - including time needed to get on the train
        /// 
        /// If the station isn't in the dictionary yet, this means no trip from this station has been already found.
        ///
        /// Also known as 'S' in the paper
        ///
        /// </summary>
        private readonly Dictionary<(uint, uint), ParetoFrontier<T>> _stationJourneys =
            new Dictionary<(uint, uint), ParetoFrontier<T>>();


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
        private readonly Dictionary<ulong, ParetoFrontier<T>> _tripJourneys =
            new Dictionary<ulong, ParetoFrontier<T>>();


        ///  <summary>
        ///  Create a new ProfiledConnectionScan algorithm.
        ///  </summary>
        public ProfiledConnectionScan(ScanSettings<T> settings)
        {
            settings.SanityCheck();

            _tdb = settings.TransitDb;
            _targetLocations = new List<(uint, uint)>();
            foreach (var (target, journey) in settings.TargetStop)
            {
                if (journey != null)
                {
                    throw new ArgumentException("PCS does not support target location journeys.");
                }

                _targetLocations.Add(target);
            }

            _departureLocations = new List<(uint, uint)>();
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

            _connectionsProvider = _tdb.ConnectionsDb;

            _comparator = settings.Comparator;
            _empty = new ParetoFrontier<T>(_comparator);
            _statsFactory = settings.StatsFactory;
            _transferPolicy = settings.TransferPolicy;
            _possibleJourney = settings.ExampleJourney;
            _filter = settings.Filter;
            _filter?.CheckWindow(_earliestDeparture, _lastArrival);
        }


        public List<Journey<T>> CalculateJourneys()
        {
            var enumerator = _connectionsProvider.GetDepartureEnumerator();

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
        private bool IntegrateBatch(ConnectionsDb.DepartureEnumerator enumerator)
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
            if (_targetLocations.Contains(c.DepartureStop))
            {
                return;
            }

            if (_departureLocations.Contains(c.ArrivalStop))
            {
                return;
            }

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
             
             The first one is the trip table, when we come across the predecessor of this Connection, 
             it'll know wat to do best
             */
            _tripJourneys[c.TripId] = journeys;

            // And ofc, we have a pretty good way out from the departure stop as well
            if (!_stationJourneys.ContainsKey(c.DepartureStop))
            {
                _stationJourneys[c.DepartureStop] = new ParetoFrontier<T>(_comparator);
            }

            _stationJourneys[c.DepartureStop].AddAllToFrontier(journeys.Frontier);


            /*We can arrive here quite optimally.
             This means that we can walk from other locations to here*/
            // Adds foohpats for each non-dominated journey 
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
            foreach (var targetLocation in _targetLocations)
            {
                // ReSharper disable once InvertIf
                if (Equals(c.ArrivalStop, targetLocation))
                {
                    // We are at a possible target location
                    // No real need to walk
                    var arrivingJourney = new Journey<T>
                    (targetLocation, c.ArrivalTime, _statsFactory.EmptyStat(),
                        Journey<T>.ProfiledScanJourney);
                    var journey = arrivingJourney.ChainBackward(c);
                    return journey;
                }
            }


            // TODO Incorporate intermodality
            return Journey<T>.InfiniteJourney;
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

            var pareto = _tripJourneys[key];
            var frontier = pareto.Frontier;
            for (var i = 0; i < frontier.Count; i++)
            {
                var newFrontier = frontier[i].ChainBackward(c);
                if (FilterJourney(newFrontier))
                {
                    frontier[i] = newFrontier;
                }
                else
                {
                    frontier.RemoveAt(i);
                    i--;
                }
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

            // We get all possible, pareto optimal journeys departing here...
            var pareto = _stationJourneys[c.ArrivalStop];
            // .. and we extend them with c. What is non-dominated, we return
            return pareto.ExtendFrontier(_tdb, c, _transferPolicy);
        }


        /// <summary>
        /// When a departure stop can be reached by a new journey, each close by stop can be reached via walking too
        /// This method creates all those footpaths and transfers 
        /// </summary>
        /// <param name="journeys"></param>
        /// <param name="cDepartureStop"></param>
        private void UpdateFootpaths(ParetoFrontier<T> journeys, (uint localTileId, uint localId) cDepartureStop)
        {
            // TODO incorporate intermodality
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

        /// <summary>
        /// Sometimes, we know that a journey will never be taken.
        ///
        /// For example, if one journey is already known to work,
        /// we know that it has no use to keep track of a journey which performs worse, as it'll be pruned later on anyway
        ///
        /// This helps in limiting the growth of the trees
        ///
        /// Returns false if no need to add the journey
        /// </summary>
        /// <returns></returns>
        private bool FilterJourney(Journey<T> j)
        {
            if (_possibleJourney == null) return true;
            var duel = _comparator.ADominatesB(_possibleJourney, j);
            return duel >= 0;
        }
    }
}