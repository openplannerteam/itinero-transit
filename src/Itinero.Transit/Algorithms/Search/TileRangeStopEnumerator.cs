using System.Collections;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Data.Tiles;

namespace Itinero.Transit.Algorithms.Search
{
    /// <summary>
    /// An enumerator that enumerates all stops in a tile range.
    /// </summary>
    internal class TileRangeStopEnumerable : IEnumerable<IStop>
    {
        private readonly List<IStopsReader> _stopsDb;
        private readonly (double minLon, double minLat, double maxLon, double maxLat) _box;

        public TileRangeStopEnumerable(IStopsReader stopsDb,
            (double minLon, double minLat, double maxLon, double maxLat) box)
        {
            _stopsDb = stopsDb.FlattenedUnderlyingDatabases();
            _box = box;
        }

        public IEnumerator<IStop> GetEnumerator()
        {
            var enumerators = new List<IEnumerator<IStop>>();
            foreach (var stopsReader in _stopsDb)
            {
                var enumerator = new TileRangeStopEnumerator(stopsReader, _box);
                enumerators.Add(enumerator);
            }

            return new EnumeratorAggregator<IStop>(enumerators.GetEnumerator());
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
            private readonly IStopsReader _stopsDbReader;
            private readonly TileRangeLocationEnumerable.TileRangeLocationEnumerator _tileRangeLocationEnumerator;

            public TileRangeStopEnumerator(IStopsReader stopsDbReader,
                (double minLon, double minLat, double maxLon, double maxLat) box)
            {
                _stopsDbReader = stopsDbReader;
                var range = new TileRange(box, stopsDbReader.StopsDb.StopLocations.Zoom);
                var tileRangeLocationEnumerable = stopsDbReader.StopsDb.StopLocations.GetTileRangeEnumerator(range);

                _tileRangeLocationEnumerator = tileRangeLocationEnumerable.GetEnumerator();
            }

            public bool MoveNext()
            {
                if (!_tileRangeLocationEnumerator.MoveNext()) return false;
                var current = _tileRangeLocationEnumerator.Current;

                return _stopsDbReader.MoveTo(new LocationId(0, current.tileId, current.localId));
            }

            public void Reset()
            {
                _tileRangeLocationEnumerator.Reset();
                _stopsDbReader.Reset();
            }

            public TileRangeLocationEnumerable.TileRangeLocationEnumerator TileRangeLocationEnumerator =>
                _tileRangeLocationEnumerator;

            public IStop Current => new Stop(_stopsDbReader); // enumerator and enumerable expect unique clones.

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}