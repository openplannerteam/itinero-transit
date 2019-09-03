namespace Itinero.Transit.Data.Tiles
{
    internal partial class TiledLocationIndex
    {
        /// <summary>
        /// An enumerator.
        /// </summary>
        public class Enumerator
        {
            private readonly TiledLocationIndex _index;

            public Enumerator(TiledLocationIndex index)
            {
                _index = index;

                _currentTileDataPointer = uint.MaxValue;
                _currentTileCapacity = uint.MaxValue;
                LocalId = uint.MaxValue;
                _currentTile = null;
                DataPointer = uint.MaxValue;
                Latitude = double.MaxValue;
                Longitude = double.MaxValue;
                TileId = uint.MaxValue;
            }

            private Tile _currentTile;
            private uint _currentTileDataPointer;
            private uint _currentTileCapacity;
            private uint _t;

            /// <summary>
            /// Resets this enumerator.
            /// </summary>
            public void Reset()
            {
                _currentTileDataPointer = uint.MaxValue;
                _currentTileCapacity = uint.MaxValue;
                _t = uint.MaxValue;
                LocalId = uint.MaxValue;
            }

            /// <summary>
            /// Moves this enumerator to the given location.
            /// </summary>
            /// <param name="localTileId">The tile id of the location.</param>
            /// <param name="localId">The local id of the location.</param>
            public bool MoveTo(uint localTileId, uint localId)
            {
                var (tileDataPointer, t, capacity) = _index.GetTile(localTileId);
                _t = t;
                
                if (localId >= capacity)
                { // local id doesn't exist.
                    return false;
                }
                var tile = Tile.FromLocalId(localTileId, _index.Zoom);
                
                var (longitude, latitude, hasData) = _index.GetEncodedLocation(tileDataPointer + localId, tile);
                if (!hasData)
                { // no data found at location.
                    return false;
                }

                _currentTile = tile;
                _currentTileCapacity = capacity;
                _currentTileDataPointer = tileDataPointer;
                Latitude = latitude;
                Longitude = longitude;
                LocalId = localId;
                TileId = localTileId;
                DataPointer = _currentTileDataPointer + localId;

                return true;
            }

            /// <summary>
            /// Move to the next location.
            /// </summary>
            /// <returns>True if there is a location available, false otherwise.</returns>
            public bool MoveNext()
            {
                if (_currentTileDataPointer == TileNotLoaded)
                { // first move, move to first tile and first pointer.
                    if (_index._tilesCount == 0)
                    { // no data.
                        return false;
                    }

                    // first tile exists, get its data.
                    _t = 0;
                    var localTileId = _index.GetLocalTileId(_t);
                    (_currentTileDataPointer, _, _currentTileCapacity) = _index.GetTile(localTileId);
                    
                    // if the tile is empty, keep moving until a non-empty tile is found.
                    while (_currentTileCapacity <= 0)
                    {
                        _t++;
                        localTileId = _index.GetLocalTileId(_t);
                        if (localTileId == TileNotLoaded) return false; // this was the last tile.
                        (_currentTileDataPointer, localTileId, _currentTileCapacity) = _index.GetTile(localTileId);
                    }

                    _currentTile = Tile.FromLocalId(localTileId, _index.Zoom);
                    TileId = localTileId;
                    LocalId = 0;
                }
                else
                { // there is a current tile.
                    if (LocalId == _currentTileCapacity - 1)
                    { // this was the last location in this tile, move to the next tile.
                        uint localTileId;
                        do
                        {
                            _t++;
                            localTileId = _index.GetLocalTileId(_t);
                            if (localTileId == TileNotLoaded) return false; // this was the last tile.
                            (_currentTileDataPointer, _, _currentTileCapacity) = _index.GetTile(localTileId);
                        } while (_currentTileCapacity <= 0);

                        _currentTile = Tile.FromLocalId(localTileId, _index.Zoom);
                        TileId = localTileId;
                        LocalId = 0;
                    }
                    else
                    {
                        // move to the next location.
                        LocalId++;
                    }
                }

                var (longitude, latitude, hasData) = _index.GetEncodedLocation(_currentTileDataPointer + LocalId, _currentTile);
                if (!hasData)
                {
                    return MoveNext();
                }

                Longitude = longitude;
                Latitude = latitude;
                DataPointer = _currentTileDataPointer + LocalId;

                return true;
            }
            
            /// <summary>
            /// Gets the data pointer.
            /// </summary>
            public uint DataPointer { get; private set; }
            
            /// <summary>
            /// Gets the tile id.
            /// </summary>
            public uint TileId { get; private set; }
            
            /// <summary>
            /// Gets the local id.
            /// </summary>
            public uint LocalId { get; private set; }
            
            /// <summary>
            /// Gets the latitude.
            /// </summary>
            public double Latitude { get; private set; }
            
            /// <summary>
            /// Gets the longitude.
            /// </summary>
            public double Longitude { get; private set; }
        }
    }
}