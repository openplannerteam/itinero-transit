using System.Collections;
using System.Collections.Generic;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Algorithms.Mergers
{
    // TODO WRITE UNIT TESTS
    /// <summary>
    /// A route is a collection of trips, so that the trips have the same stops in the same order.
    /// The exact time between the stops is not considered.
    /// </summary>
    public class RouteMerger
    {
        private readonly Route _genesis = new Route(null, StopId.Invalid);

        private Dictionary<TripId, Route> _tripToRoute = new Dictionary<TripId, Route>();


        /// <summary>
        /// This dictionary tracks how next steps can be made, e.g. if we have route
        /// (A -> B), it keeps track that one can extends it to C and D, and if a route representing this already exists
        /// The dictionary would thus look like:
        ///
        /// { (A -> B, C) --> (A -> B -> C), (A -> B, D) --> (A -> B -> D) }
        /// 
        /// </summary>
        private Dictionary<(Route, StopId), Route> _nextSteps = new Dictionary<(Route, StopId), Route>();

        public RouteMerger()
        {
            
        }

        public RouteMerger(IEnumerable<Connection> cs)
        {
            AddConnections(cs);
        }

        private Route ExtendRoute(Route r, StopId stop)
        {
            if (_nextSteps.TryGetValue((r, stop), out var nextStep))
            {
                // Route already exists 
                return nextStep;
            }

            nextStep = new Route(r, stop);
            _nextSteps[(r, stop)] = nextStep;
            return nextStep;
        }

        public void AddConnections(IEnumerable<Connection> cs)
        {
            foreach (var connection in cs)
            {
                AddConnection(connection);
            }
        }

        public void AddConnection(Connection c)
        {
            var arrStop = c.ArrivalStop;
            if (_tripToRoute.TryGetValue(c.TripId, out var foundRoute))
            {
                // This route already exists, but it is extended
                // This means that it should be changed to a new route
                _tripToRoute[c.TripId] = ExtendRoute(foundRoute, arrStop);
            }
            else
            {
                // This is an entirely new route
                var seed = ExtendRoute(_genesis, c.DepartureStop);
                var route = ExtendRoute(seed, c.ArrivalStop);
                _tripToRoute[c.TripId] = route;
            }
        }

        public Dictionary<TripId, Route> GetTripToRoutes()
        {
            return _tripToRoute;
        }

        public Dictionary<Route, List<TripId>> GetRouteToTrips()
        {
        
            var result = new Dictionary<Route, List<TripId>>();
            foreach (var kv in _tripToRoute)
            {
                if (!result.ContainsKey(kv.Value))
                {
                    result[kv.Value] = new List<TripId>();
                }
                result[kv.Value].Add(kv.Key);
            }

            return result;
        }
    }

    /// <summary>
    /// A route is a recursive structure, similar to a Journey
    /// </summary>
    public class Route : IEnumerable<StopId>
    {
        public readonly Route PreviousRoute; // Null for genesis
        public readonly StopId LastStop;

        public Route(Route previousRoute, StopId lastStop)
        {
            PreviousRoute = previousRoute;
            LastStop = lastStop;
        }


        public IEnumerator<StopId> GetEnumerator()
        {
            return new RouteEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class RouteEnumerator : IEnumerator<StopId>
    {
        private Route _currentRoute;
        private readonly Route _startPoint; // of the enumeration

        public RouteEnumerator(Route currentRoute)
        {
            _startPoint = currentRoute;
        }

        public bool MoveNext()
        {
            if (_currentRoute == null)
            {
                _currentRoute = _startPoint;
                return true;
            }

            _currentRoute = _currentRoute.PreviousRoute;
            return
                _currentRoute.PreviousRoute !=
                null; // The genesis element (which is the last element) has an invalid stop id; we must thus stop one before the null pointer
        }

        public void Reset()
        {
            _currentRoute = null;
        }

        public StopId Current => _currentRoute.LastStop;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }
}