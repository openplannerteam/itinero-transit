using System;
using System.Collections;
using System.Collections.Generic;

namespace Itinero.Transit.Data.Tiles
{
    internal class TileRangeLocationEnumerable : IEnumerable<(uint tileId, uint localId, uint dataPointer)>
    {
        private readonly TiledLocationIndex _locationIndex;
        private readonly TileRange _tileRange;

        public TileRangeLocationEnumerable(TiledLocationIndex locationIndex, TileRange tileRange)
        {
            if (tileRange.Zoom != locationIndex.Zoom) throw new ArgumentException("Cannot enumerate vertices based on a tile range when it's zoom level doesn't match the graph zoom level.");
            
            _tileRange = tileRange;
            _locationIndex = locationIndex;
        }
        
        public TileRangeLocationEnumerator GetEnumerator()
        {
            return new TileRangeLocationEnumerator(this);   
        }

        IEnumerator<(uint tileId, uint localId, uint dataPointer)> IEnumerable<(uint tileId, uint localId, uint dataPointer)>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal class TileRangeLocationEnumerator : IEnumerator<(uint tileId, uint localId, uint dataPointer)>
        {
            private readonly IEnumerator<Tile> _tileEnumerator;
            private readonly TiledLocationIndex.Enumerator _locationEnumerator;

            public TileRangeLocationEnumerator(TileRangeLocationEnumerable enumerable)
            {
                var enumerable1 = enumerable;
                
                _tileEnumerator = enumerable1._tileRange.GetEnumerator();
                _locationEnumerator = enumerable1._locationIndex.GetEnumerator();
            }
            
            private uint _currentTile = uint.MaxValue;
            private uint _currentLocal = uint.MaxValue;
            private double _currentLatitude;
            private double _currentLongitude;
            
            public bool MoveNext()
            {
                if (_currentTile == uint.MaxValue)
                {
                    while (_tileEnumerator.MoveNext())
                    {
                        _currentTile = _tileEnumerator.Current.LocalId;
                        _currentLocal = 0;

                        // ReSharper disable once InvertIf
                        if (_locationEnumerator.MoveTo(_currentTile, _currentLocal))
                        {
                            _currentLatitude = _locationEnumerator.Latitude;
                            _currentLongitude = _locationEnumerator.Longitude;

                            return true;
                        }
                    }

                    return false;
                }

                while (true)
                {
                    _currentLocal++;
                    if (_locationEnumerator.MoveTo(_currentTile, _currentLocal))
                    {
                        _currentLatitude = _locationEnumerator.Latitude;
                        _currentLongitude = _locationEnumerator.Longitude;

                        return true;
                    }

                    if (!_tileEnumerator.MoveNext())
                    {
                        return false;
                    }
                    _currentLocal = 0;
                    _currentTile = _tileEnumerator.Current.LocalId;
                    if (_locationEnumerator.MoveTo(_currentTile, _currentLocal))
                    {
                        _currentLatitude = _locationEnumerator.Latitude;
                        _currentLongitude = _locationEnumerator.Longitude;

                        return true;
                    }
                }
            }

            public void Reset()
            {
                _tileEnumerator.Reset();
            }

            public (uint tileId, uint localId, uint dataPointer) Current => (_currentTile, _currentLocal, _locationEnumerator.DataPointer);

            object IEnumerator.Current => Current;

            public double Longitude => _currentLongitude;

            public double Latitude => _currentLatitude;

            public void Dispose()
            {
                
            }
        }
    }
}