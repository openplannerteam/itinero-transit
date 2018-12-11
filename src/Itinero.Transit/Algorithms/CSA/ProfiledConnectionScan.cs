using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Itinero.Transit.Logging;

namespace Itinero.IO.LC
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
        private readonly ConnectionsDb _connectionsProvider;
        private readonly UnixTime _earliestDeparture, _lastArrival;
        private readonly (uint, uint) _departureLocation, _targetLocation;

        private readonly ProfiledStatsComparator<T> _comparator;

        private readonly T _statsFactory;

        // Indicates if connections can not be taken due to external reasons (e.g. earlier scan)
        private readonly IConnectionFilter _filter;

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


        public ProfiledConnectionScan(
            (uint, uint) departureStop,
            (uint, uint) arrivalStop,
            DateTime earliestDeparture, DateTime lastArrival,
            Profile<T> profile,
            IConnectionFilter filter = null) :
            this(departureStop, arrivalStop, earliestDeparture.ToUnixTime(), lastArrival.ToUnixTime(), profile, filter)
        {
        }

        ///  <summary>
        ///  Create a new ProfiledConnectionScan algorithm.
        ///  </summary>
        public ProfiledConnectionScan(
            (uint, uint) departureStop,
            (uint, uint) arrivalStop,
            UnixTime earliestDeparture, UnixTime lastArrival,
            Profile<T> profile,
            IConnectionFilter filter = null)
        {
            if (Equals(departureStop, arrivalStop))
            {
                throw new ArgumentException("Target and destination are the same");
            }

            if (earliestDeparture >= lastArrival)
            {
                throw new ArgumentException(
                    "Departure time falls after arrival time. Do you intend to travel backwards in time? If so, lend me that time machine!");
            }

            _departureLocation = departureStop;
            _targetLocation = arrivalStop;

            _earliestDeparture = earliestDeparture;
            _lastArrival = lastArrival;

            _connectionsProvider = profile.ConnectionsDb;
            _comparator = profile.ProfileComparator;
            _empty = new ParetoFrontier<T>(_comparator);
            _statsFactory = profile.StatsFactory;
            _transferPolicy = profile.WalksGenerator;
            _filter = filter;
            filter?.CheckWindow(_earliestDeparture, _lastArrival);
            Log.Information($"Searching PCS from {_departureLocation} to {_targetLocation}," +
                            $" time window is {DateTimeExtensions.FromUnixTime(_earliestDeparture):HH:mm} - {DateTimeExtensions.FromUnixTime(_lastArrival):HH:mm}");
        }


        public IEnumerable<Journey<T>> CalculateJourneys()
        {
            var enumerator = _connectionsProvider.GetDepartureEnumerator();

            // Move the enumerator after the last arrival time
            enumerator.MovePrevious();
            while (enumerator.DepartureTime > _lastArrival)
            {
                if (!enumerator.MovePrevious())
                {
                    throw new Exception(
                        "Could not calculate PCS: departure time not found. Either to little connections are loaded in the database, or the query is to far in the future or in the past");
                }
            }


            while (enumerator.DepartureTime >= _earliestDeparture)
            {
                IntegrateBatch(enumerator);
            }

            // We have scanned all connections in the given timeframe
            // Time to extract the wanted journeys
            if (!_stationJourneys.ContainsKey(_departureLocation))
            {
                return null;
            }

            var journeys = _stationJourneys[_departureLocation].Frontier;
            var revJourneys = new List<Journey<T>>();
            foreach (var j in journeys)
            {
                revJourneys.Add(j.Reversed());
            }

            return revJourneys;
        }


        /// <summary>
        /// Integrates all connections of the enumerator where the departure time is the current departure time
        /// </summary>
        /// <param name="enumerator"></param>
        private void IntegrateBatch(ConnectionsDb.DepartureEnumerator enumerator)
        {
            var depTime = enumerator.DepartureTime;
            var tripId = enumerator.Id;
            do
            {
                IntegrateConnection(enumerator);
                if (!enumerator.MovePrevious())
                {
                    throw new Exception("Enumerator depleted");
                }

                if (enumerator.Id == tripId)
                {
                    throw new Exception("Stuck in a loop: we have reached the first element of the database");
                }

                tripId = enumerator.Id;
            } while (depTime == enumerator.DepartureTime);
        }


        /// <summary>
        /// Looks to this single connection, the actual PCS step
        /// </summary>
        /// <param name="c"></param>
        private void IntegrateConnection(IConnection c)
        {
            if (c.DepartureStop == _targetLocation)
            {
                return;
            }

            if (c.ArrivalStop == _departureLocation)
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

            /*
             * And ofc, we have a pretty good way out from the departure stop as well
             */
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
            // ReSharper disable once InvertIf
            if (Equals(c.ArrivalStop, _targetLocation))
            {
                // We are at our target location
                // No real need to walk
                var arrivingJourney = new Journey<T>
                    (_targetLocation, c.ArrivalTime, _statsFactory.EmptyStat());
                var journey = arrivingJourney.ChainBackward(c);
                return journey;
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

            // We get all possible, pareto optimal journeys departing here...
            var pareto = _stationJourneys[c.ArrivalStop];
            // .. and we extend them with c. What is non-dominated, we return
            return pareto.ExtendFrontier(c, _transferPolicy);
        }


        /// <summary>
        /// When a departure stop can be reached by a new journey, each closeby stop can be reached via walking too
        /// This method creates all those footpaths and transfers 
        /// </summary>
        /// <param name="journeys"></param>
        /// <param name="cDepartureStop"></param>
        private void UpdateFootpaths(ParetoFrontier<T> journeys, (uint localTileId, uint localId) cDepartureStop)
        {
            // TODO incorporate intermodality
        }


        public ParetoFrontier<T> PickBestJourneys(Journey<T> j, ParetoFrontier<T> a, ParetoFrontier<T> b)
        {
            if (a.Frontier.Count == 0 && b.Frontier.Count == 0)
            {
                if (j == Journey<T>.InfiniteJourney)
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
    }
}