using System.Collections;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Tiles;

namespace Itinero.Transit.Algorithms.Search
{
    /// <summary>
    /// An enumerator that enumerates all stops in a tile range.
    /// </summary>
    internal class TileRangeStopEnumerable : IEnumerable<IStop>
    {
        private readonly IStopsReader _stopsDb;
        private readonly TileRangeLocationEnumerable _tileRangeLocationEnumerable;

        public TileRangeStopEnumerable(IStopsReader stopsDb, TileRangeLocationEnumerable tileRangeLocationEnumerable)
        {
            _stopsDb = stopsDb;
            _tileRangeLocationEnumerable = tileRangeLocationEnumerable;
        }
        
        public TileRangeStopEnumerator GetEnumerator()
        {
            return new TileRangeStopEnumerator(this);
        }

        IEnumerator<IStop> IEnumerable<IStop>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal class TileRangeStopEnumerator : IEnumerator<IStop>
        {
            private readonly TileRangeLocationEnumerable.TileRangeLocationEnumerator _tileRangeLocationEnumerator;
            private readonly IStopsReader _stopsDbReader;

            public TileRangeStopEnumerator(TileRangeStopEnumerable enumerable)
            {
                _tileRangeLocationEnumerator = enumerable._tileRangeLocationEnumerable.GetEnumerator();
                _stopsDbReader = enumerable._stopsDb;
            }
            
            public bool MoveNext()
            {
                if (!_tileRangeLocationEnumerator.MoveNext()) return false;
                var current = _tileRangeLocationEnumerator.Current;

                if (!_stopsDbReader.MoveTo((current.tileId, current.localId))) return false;

                return true;
            }

            public void Reset()
            {
                _tileRangeLocationEnumerator.Reset();
                _stopsDbReader.Reset();
            }

            public TileRangeLocationEnumerable.TileRangeLocationEnumerator TileRangeLocationEnumerator => _tileRangeLocationEnumerator;

            public IStop Current => new Stop(_stopsDbReader); // enumerator and enumerable expect unique clones.

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                
            }
        }
    }
}