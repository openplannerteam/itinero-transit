using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Data.Compacted
{
    /// <summary>
    /// The compacted connections DB _generates_ connections based on routes and times.
    /// It contains a collection of time schedules (sorted by first departure time), and the route they are on
    /// The enumerator for them keeps track of what schedules are 'open' in order to generate connections.
    ///
    /// The ID's of the connections are generated: they contain an internal id (which is used to retrieve the route and time schedule) and a counter indicating the position in the sequence
    /// 
    /// </summary>
    public class CompactedConnectionsDb : IConnectionsDb
    {
        /// <summary>
        /// Sorted by firstDeparture
        /// </summary>
        private List<FullTrip> _allTrips = new List<FullTrip>();

        public IEnumerable<uint> DatabaseIds { get; }


        private CompactedConnectionsDb(IEnumerable<FullTrip> allTrips)
        {
            _allTrips.AddRange(allTrips);
        }

        public CompactedConnectionsDb()
        {
        }


        
        public ulong EarliestDate
        {
            get
            {
                if (_allTrips.Any())
                {
                    return _allTrips[0].FirstDeparture;
                }

                return ulong.MaxValue;
            }
        }

        public ulong LatestDate { get; private set; }

        public void PostProcess()
        {
            _allTrips.Sort(new FullTripComparer());
            LatestDate = 0ul;
            foreach (var trip in _allTrips)
            {
                var latest = trip.FirstDeparture + trip.TimeSchedule.Latest();
                if (LatestDate < latest)
                {
                    LatestDate = latest;
                }
            }
        }

        
        
        
        public IEnumerator<Connection> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool TryGet(ConnectionId id, out Connection t)
        {
            throw new NotImplementedException();
        }

        public bool TryGetId(string globalId, out ConnectionId id)
        {
            throw new NotImplementedException();
        }



        
        
        
        

        public IConnectionEnumerator GetEnumeratorAt(ulong departureTime)
        {
            throw new NotImplementedException();
        }

        public IConnectionsDb Clone()
        {
            return new CompactedConnectionsDb(_allTrips);
        }
    }

    internal class ConnectionEnumerator : IEnumerator<Connection>
    {
        
        
        
        public bool MoveNext()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public Connection Current { get; }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}