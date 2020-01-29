using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Logging;

namespace Itinero.Transit.Data.Simple
{
    public class SimpleConnectionsDb :
        SimpleDb<ConnectionId, Connection>, IConnectionsDb, IClone<SimpleConnectionsDb>
    {
        public SimpleConnectionsDb(uint dbId) : base(dbId)
        {
        }

        public SimpleConnectionsDb(SimpleDb<ConnectionId, Connection> copyFrom) : base(copyFrom)
        {
        }


        public void PostProcess()
        {
            Sort();
        }

        /// <summary>
        /// Sort the data by departure time of the connections.
        /// </summary>
        private void Sort()
        {

            if (!Data.Any())
            {
                // Hmm, empty db... Not a lot to prepocess
                return;
            }
            Data.Sort(Connection.SortByDepartureTime);
            
            // Edge case: a train has a (theoretical) stop time of 0seconds in one stop
            // Then it can happen that this stop jumps after the departure from that stop, e.g.
            // A -> B
            // C -> D
            // B -> C
            
            // That is of course incorrect
            
            // We fix this by doing a check for those cases and correcting them
            // We run over the data (sorted by time)
            // For every discrete departure time, we build a histogram {trip --> (connection, index in list)}
            // Then, we check every trip and swap if necessary


            var trips = new Dictionary<TripId, List<(int index, Connection c)>>();
            var currentDepartureTime = Data.First().DepartureTime;
            for (var i = 0; i < Data.Count; i++)
            {
                var connection = Data[i];
                if (currentDepartureTime != connection.DepartureTime)
                {
                    // We have reached a new era
                    // Let's correct the previous one

                    foreach (var trip in trips)
                    {
                        CorrectTripSingleDepartureTime(trip.Value);
                    }

                    currentDepartureTime = connection.DepartureTime;
                }
                
                
                if (!trips.ContainsKey(connection.TripId))
                {
                    trips[connection.TripId] = new List<(int index, Connection c)>();
                }
                
                trips[connection.TripId].Add((i, connection));
            }
            // Let's not forget to handle the last connections...
            foreach (var trip in trips)
            {
                CorrectTripSingleDepartureTime(trip.Value);
            }
            
        }

        /// <summary>
        /// Corrects the trip.
        /// We assume the triplist is ordered by index
        /// Very statefull: the triplist is cleared upon exiting this method
        /// </summary>
        /// <param name="trip"></param>
        private void CorrectTripSingleDepartureTime(IList<(int index, Connection c)> trip)
        {
            if (trip.Count == 0)
            {
                return;
            }

            if (trip.Count == 1)
            {
                trip.Clear();
                return;
            }
            
            _byArrivalStop.Clear();
            _connectionsOrdered.Clear();

            foreach (var c in trip)
            {
                if (_byArrivalStop.ContainsKey(c.c.ArrivalStop))
                {
                    Log.Warning($"A broken trip was found: {trip[0].c.TripId}: Could not correct single departure time.");
                    return;
                }
                _byArrivalStop.Add(c.c.ArrivalStop,c);
            }

            do
            {
                // We search the first connection: this is the connection of which the departureStop is _not_ in the dictionary
                // We add that one to the connectionsOrderedList
                var oneFound = false;
                foreach (var c in trip)
                {
                    if (_byArrivalStop.ContainsKey(c.c.DepartureStop))
                    {
                        // Not the first
                        continue;
                    }

                    if (!_byArrivalStop.ContainsKey(c.c.ArrivalStop))
                    {
                        // Already handled
                        continue;
                    }
                    
                    _connectionsOrdered.Add(c);
                    _byArrivalStop.Remove(c.c.ArrivalStop);
                    oneFound = true;
                }

                if (!oneFound)
                {
                    // In normal circumstances, this should never be triggered
                    // It is merely here to prevent infinite loops
                    Log.Warning($"A broken trip was found: {trip[0].c.TripId}: Could not correct single departure time.");
                    return;
                }

 
                // And we repeat this until there is no connection left
            } while (_byArrivalStop.Any());
            
            // At this point, we have the connections in the order that they have to be, for example
            // [(A -> B, 1), (B -> C, 0)] clearly showing the wrong index order here
                
                
            // The first element of _connectionsOrdered should be placed at the lowest index found in the list
            // We can neatly match them as the indices can be read (in order) from the triplist!
            


            for (var i = 0; i < trip.Count; i++)
            {
                var indexToPlace = trip[i].index;
                var connectionToPlace = _connectionsOrdered[i].c;
                Data[indexToPlace] = connectionToPlace;
            }

            
            trip.Clear();
        }
        
        // Only used by CorrectTripSingleDepartureTime, infinitely reused
        private List<(int index, Connection c)> _connectionsOrdered = new List<(int index, Connection c)>();
        // Only used by CorrectTripSingleDepartureTime, infinitely reused
        private Dictionary<StopId, (int index, Connection c)> _byArrivalStop = new Dictionary<StopId, (int index, Connection c)>();

        /// <summary>
        /// Gives the departure time of the first connection in the DB, sorted by departure time
        /// </summary>
        public ulong EarliestDate => First()?.DepartureTime ?? ulong.MaxValue;

        /// <summary>
        /// Gives the departure time of the last connection in the DB, sorted by departure time
        /// </summary>
        public ulong LatestDate => Last()?.DepartureTime ?? ulong.MinValue;

        /// <summary>
        /// Returns the index of the first connection which departs after the given datetime
        /// </summary>
        /// <returns></returns>
        public int IndexOfFirst(ulong departureTime)
        {
            // We perform a binary search to get this first connection
            var l = 0;
            var r = Data.Count;
            while (l < r)
            {
                var m = (l + r) / 2;
                var mDep = Data[m].DepartureTime;
                if (mDep < departureTime)
                {
                    l = m + 1;
                }
                else
                {
                    r = m;
                }
            }

            return l;
        }

        internal ulong GetDepartureTimeOf(uint index)
        {
            return Data[(int) index].DepartureTime;
        }

        public IConnectionEnumerator GetEnumeratorAt(ulong departureTime)
        {
            return new SimpleConnectionEnumerator(this, Data.Count, IndexOfFirst(departureTime));
        }

        public SimpleConnectionsDb Clone()
        {
            return new SimpleConnectionsDb(this);
        }

        IConnectionsDb IClone<IConnectionsDb>.Clone()
        {
            return Clone();
        }


        internal class SimpleConnectionEnumerator : IConnectionEnumerator
        {
            private readonly int _count;
            private readonly uint _dbId;
            private int _next;
            private SimpleConnectionsDb _fallback;
            public ulong CurrentTime { get; private set; }

            public SimpleConnectionEnumerator(SimpleConnectionsDb fallback, int count, int startConnection)
            {
                _dbId = fallback.DatabaseId;
                _fallback = fallback;
                _count = count;
                _next = startConnection;
            }


            public bool MoveNext()
            {
                if (_count == 0 || _next >= _count)
                {
                    CurrentTime = ulong.MaxValue;
                    return false;
                }

                CurrentTime = _fallback.GetDepartureTimeOf((uint) _next);
                Current = new ConnectionId(_dbId, (uint) _next);
                _next++;
                return true;
            }

            public bool MovePrevious()
            {
                if (_next < 0 || _count == 0)
                {
                    CurrentTime = ulong.MinValue;
                    return false;
                }

                if (_next >= _count)
                {
                    _next = _count - 1;
                }

                CurrentTime = _fallback.GetDepartureTimeOf((uint) _next);
                Current = new ConnectionId(_dbId, (uint) _next);

                _next--;
                return true;
            }

            public void Reset()
            {
                _next = 0;
                CurrentTime = _fallback.GetDepartureTimeOf((uint) _next);
            }

            [Pure] public ConnectionId Current { get; private set; }
            [Pure] object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}