using System.Collections;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.Tiles;

namespace Itinero.Transit.Algorithms.Search
{
    /// <summary>
    /// An enumerator that enumerates all stops in a tile range.
    /// </summary>
    internal class TileRangeStopEnumerable : IEnumerable<IStop>
    {
        private readonly StopsDb _stopsDb;
        private readonly (double minLon, double minLat, double maxLon, double maxLat) _box;

        public TileRangeStopEnumerable(StopsDb stopsDb,
            (double minLon, double minLat, double maxLon, double maxLat) box)
        {
            _stopsDb = stopsDb;
            _box = box;
        }

        public IEnumerator<IStop> GetEnumerator()
        {
            return new TileRangeStopEnumerator(_stopsDb, _box);
        }

        IEnumerator<IStop> IEnumerable<IStop>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        private class TileRangeStopEnumerator : IEnumerator<IStop>
        {
            private readonly StopsDb.StopsDbReader _stopsDbReader;
            private readonly TileRangeLocationEnumerable.TileRangeLocationEnumerator _tileRangeLocationEnumerator;

            public TileRangeStopEnumerator(StopsDb stopsDb,
                (double minLon, double minLat, double maxLon, double maxLat) box)
            {
                _stopsDbReader = stopsDb.GetReader();
                var range = new TileRange(box, stopsDb.StopLocations.Zoom);
                var tileRangeLocationEnumerable = stopsDb.StopLocations.GetTileRangeEnumerator(range);

                _tileRangeLocationEnumerator = tileRangeLocationEnumerable.GetEnumerator();
            }

            public bool MoveNext()
            {
                if (!_tileRangeLocationEnumerator.MoveNext()) return false;
                var current = _tileRangeLocationEnumerator.Current;

                return _stopsDbReader.MoveTo(new StopId(_stopsDbReader.StopsDb.DatabaseId, current.tileId, current.localId));
            }

            public void Reset()
            {
                _tileRangeLocationEnumerator.Reset();
                _stopsDbReader.Reset();
            }

            public IStop Current => new Stop(_stopsDbReader); // enumerator and enumerable expect unique clones.

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}