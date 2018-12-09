// The MIT License (MIT)

// Copyright (c) 2018 Anyways B.V.B.A.

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.CompilerServices;
using Reminiscence.Arrays;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]
namespace Itinero.Transit.Data.Tiles
{
    internal class TiledLocationIndex
    {
        private readonly int _zoom;
        
        private const byte DefaultTileCapacityInBytes = 0; 
        private const int CoordinateSizeInBytes = 3; // 3 bytes = 24 bits = 4096 x 4096, the needed resolution depends on the zoom-level, higher, less resolution.
        const int TileResolutionInBits = CoordinateSizeInBytes * 8 / 2;

        private readonly ArrayBase<byte> _tileIndex;
        private readonly ArrayBase<byte> _locations; // holds stop locations, encode relative to the tile they are in.
        private const uint TileNotLoaded = uint.MaxValue;
        private uint _tileIndexPointer = 0;
        private uint _tileDataPointer = 0;
        
        /// <summary>
        /// Creates a new location index.
        /// </summary>
        /// <param name="zoom"></param>
        public TiledLocationIndex(int zoom = 14)
        {
            _zoom = zoom;
            
            _tileIndex = new MemoryArray<byte>(0);
            _locations = new MemoryArray<byte>(0);
        }

        /// <summary>
        /// Adds a new latitude longitude pair.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude</param>
        /// <returns>The tile id, the local id and a data pointer.</returns>
        public (uint tileId, uint localId, uint dataPointer) Add(double longitude, double latitude)
        {
            // get the local tile id.
            var tile = Tile.WorldToTile(longitude, latitude, _zoom);
            var tileId = tile.LocalId;

            // try to find the tile.
            var (tileDataPointer, tileIndexPointer, capacity) = FindTile(tileId);
            if (tileDataPointer == TileNotLoaded)
            { // create the tile if it doesn't exist yet.
                (tileDataPointer, tileIndexPointer, capacity) = AddTile(tileId);
            }

            // find or create a place to store the location.
            uint nextEmpty;
            if (capacity == 0 ||
                GetEncodedLocation((uint)(tileDataPointer + capacity - 1), tile).hasData)
            { // tile is maximum capacity.
                (tileDataPointer, capacity) = IncreaseCapacityForTile(tileIndexPointer, tileDataPointer);
                nextEmpty = (uint)(tileDataPointer + (capacity / 2));
            }
            else
            { // find the last empty slot.
                nextEmpty = (uint)(tileDataPointer + capacity - 1);
                if (nextEmpty > _tileDataPointer)
                {
                    for (var p = nextEmpty - 1; p >= tileDataPointer; p--)
                    {
                        if (GetEncodedLocation(p, tile).hasData)
                        {
                            // non-empty slot found.
                            break;
                        }

                        nextEmpty = p;
                    }
                }
            }
            
            var localId = (nextEmpty - tileDataPointer);
            
            // set the vertex data.
            SetEncodedLocation(nextEmpty, tile, longitude, latitude);

            return (tileId, localId, nextEmpty);
        }

        /// <summary>
        /// Gets the zoom.
        /// </summary>
        public int Zoom => _zoom;

        /// <summary>
        /// A delegate to notify listeners that a block of locations has moved.
        /// </summary>
        /// <param name="from">The from pointer.</param>
        /// <param name="to">The to pointer.</param>
        /// <param name="count">The number of locations that moved.</param>
        public delegate void MovedDelegate(uint from, uint to, uint count);
        
        /// <summary>
        /// Gets or sets the moved delegate.
        /// </summary>
        public MovedDelegate Moved { get; set; }
        
        /// <summary>
        /// Finds a tile in the tile index.
        /// </summary>
        /// <param name="localTileId">The local tile id.</param>
        /// <returns>The meta-data about the tile and its data.</returns>
        private (uint tileDataPointer, uint tileIndexPointer, int capacity) FindTile(uint localTileId)
        {
            // find an allocation-less way of doing this:
            //   this is possible it .NET core 2.1 but not netstandard2.0,
            //   we can do this in netstandard2.1 normally.
            //   perhaps implement our own version of bitconverter.
            var tileBytes = new byte[4];
            for (uint p = 0; p < _tileIndex.Length - 9; p += 9)
            {
                for (var b = 0; b < 4; b++)
                {
                    tileBytes[b] = _tileIndex[p + b];
                }
                var tileId = BitConverter.ToUInt32(tileBytes, 0);
                if (tileId != localTileId) continue;
                
                for (var b = 0; b < 4; b++)
                {
                    tileBytes[b] = _tileIndex[p + b + 4];
                }

                return (BitConverter.ToUInt32(tileBytes, 0), p, 1 << _tileIndex[p + 8]);
            }

            return (TileNotLoaded, uint.MaxValue, 0);
        }

        /// <summary>
        /// Reads meta-data about a tile and its data.
        /// </summary>
        /// <param name="tileIndexPointer">The pointer to the tile in the index.</param>
        /// <returns>The meta-data about the tile and its data.</returns>
        private (uint tileDataPointer, uint localTileId, uint capacity) ReadTile(uint tileIndexPointer)
        {
            var tileBytes = new byte[4];
            for (var b = 0; b < 4; b++)
            {
                tileBytes[b] = _tileIndex[tileIndexPointer + b];
            }
            var tileId = BitConverter.ToUInt32(tileBytes, 0);
            for (var b = 0; b < 4; b++)
            {
                tileBytes[b] = _tileIndex[tileIndexPointer + b + 4];
            }

            return (BitConverter.ToUInt32(tileBytes, 0), tileId, (uint)(1 << _tileIndex[tileIndexPointer + 8]));
        }

        /// <summary>
        /// Adds a new tile.
        /// </summary>
        /// <param name="localTileId">The local tile id.</param>
        /// <returns>The meta-data about the tile and its data.</returns>
        private (uint tileDataPointer, uint tileIndexPointer, int capacity) AddTile(uint localTileId)
        {
            if (_tileIndexPointer + 9 >= _tileIndex.Length)
            {
                _tileIndex.Resize(_tileIndex.Length + 1024);
            }
            
            var tileBytes = BitConverter.GetBytes(localTileId);
            for (var b = 0; b < 4; b++)
            {
                _tileIndex[_tileIndexPointer + b] = tileBytes[b];
            }
            tileBytes = BitConverter.GetBytes(_tileDataPointer);
            for (var b = 0; b < 4; b++)
            {
                _tileIndex[_tileIndexPointer + 4 + b] = tileBytes[b];
            }
            _tileIndex[_tileIndexPointer + 9] = DefaultTileCapacityInBytes;
            var tilePointer = _tileIndexPointer;
            _tileIndexPointer += 9;
            const int capacity = 1 << DefaultTileCapacityInBytes;
            var pointer = _tileDataPointer;
            _tileDataPointer += capacity;
            return (pointer, tilePointer, capacity);
        }
        
        /// <summary>
        /// Increase capacity for the given tile.
        /// </summary>
        /// <param name="tileIndexPointer"></param>
        /// <param name="tileDataPointer"></param>
        /// <returns></returns>
        private (uint tileDataPointer, int capacity) IncreaseCapacityForTile(uint tileIndexPointer, uint tileDataPointer)
        {
            // copy current data, we assume current capacity is at max.
            
            // get current capacity and double it.
            var capacityInBits = _tileIndex[tileIndexPointer + 8];
            _tileIndex[tileIndexPointer + 8] = (byte)(capacityInBits + 1);
            var oldCapacity = 1 << capacityInBits;

            // get the current pointer and update it.
            var newTileDataPointer = _tileDataPointer;
            _tileDataPointer += (uint)(oldCapacity * 2);
            
            // update the tile data pointer in the tile index.
            var pointerBytes = BitConverter.GetBytes(newTileDataPointer); 
            for (var b = 0; b < 4; b++)
            {
                _tileIndex[tileIndexPointer + 4 + b] = pointerBytes[b];
            }
            
            // make sure locations array is the proper size.
            var length = _locations.Length;
            while ((_tileDataPointer + (oldCapacity * 2)) * CoordinateSizeInBytes >= length)
            {
                length += 1024;
            }
            if (length > _locations.Length)
            {
                var p = _locations.Length;
                _locations.Resize(length);
                for (var i = p; i < _locations.Length; i++)
                {
                    _locations[i] = byte.MaxValue;
                }
            }
            
            // copy all the data over.
            for (uint p = 0; p < oldCapacity; p++)
            {
                CopyLocations(tileDataPointer + p, newTileDataPointer + p);
            }
            
            // notify any listeners of copied blocks.
            this.Moved?.Invoke(tileDataPointer, newTileDataPointer, (uint)oldCapacity);

            return (newTileDataPointer, oldCapacity * 2);
        }

        private void CopyLocations(uint pointer1, uint pointer2)
        {
            var locationPointer1 = pointer1 * CoordinateSizeInBytes;
            var locationPointer2 = pointer2 * CoordinateSizeInBytes;

            for (var b = 0; b < CoordinateSizeInBytes; b++)
            {
                _locations[locationPointer2 + b] = _locations[locationPointer1 + b];
            }
        }
        
        
        private (double longitude, double latitude, bool hasData) GetEncodedLocation(uint pointer, Tile tile)
        {
            const int TileResolutionInBits = CoordinateSizeInBytes * 8 / 2;
            var locationPointer = pointer * (long)CoordinateSizeInBytes;

            if (locationPointer + 4 > _locations.Length)
            {
                return (0, 0, false);
            }
            
            var bytes = new byte[4];
            for (var b = 0; b < CoordinateSizeInBytes; b++)
            {
                bytes[b] = _locations[locationPointer + b];
            }

            var localCoordinatesEncoded = BitConverter.ToUInt32(bytes, 0);
            if (localCoordinatesEncoded == 16777215) // 3 bytes at max
            {
                return (0, 0, false);
            }
            var y = (int)localCoordinatesEncoded % (1 << TileResolutionInBits);
            var x = (int)localCoordinatesEncoded >> TileResolutionInBits;

            var (longitude, latitude) = tile.FromLocalCoordinates(x, y, 1 << TileResolutionInBits);

            return (longitude, latitude, true);
        }
        
        private void SetEncodedLocation(uint pointer, Tile tile, double longitude, double latitude)
        {
            var localCoordinates = tile.ToLocalCoordinates(longitude, latitude, 1 << TileResolutionInBits);
            var localCoordinatesEncoded = (localCoordinates.x << TileResolutionInBits) + localCoordinates.y;
            var localCoordinatesBits = BitConverter.GetBytes(localCoordinatesEncoded);
            var tileDataPointer = pointer * (long)CoordinateSizeInBytes;
            if (tileDataPointer + CoordinateSizeInBytes > _locations.Length)
            {
                var p = _locations.Length;
                _locations.Resize(_locations.Length + 1024);
                for (var i = p; i < _locations.Length; i++)
                {
                    _locations[i] = byte.MaxValue;
                }
            }
            for (var b = 0; b < CoordinateSizeInBytes; b++)
            {
                _locations[tileDataPointer + b] = localCoordinatesBits[b];
            }
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// An enumerator.
        /// </summary>
        public class Enumerator
        {
            private readonly TiledLocationIndex _index;

            public Enumerator(TiledLocationIndex index)
            {
                _index = index;

                _currentTileIndexPointer = uint.MaxValue;
                _currentTileDataPointer = uint.MaxValue;
                _currentTileCapacity = uint.MaxValue;
                this.LocalId = uint.MaxValue;
                _currentTile = null;
                this.DataPointer = uint.MaxValue;
                this.Latitude = double.MaxValue;
                this.Longitude = double.MaxValue;
                this.LocalTileId = uint.MaxValue;
            }

            private uint _currentTileIndexPointer;
            private Tile _currentTile;
            private uint _currentTileDataPointer;
            private uint _currentTileCapacity;

            /// <summary>
            /// Resets this enumerator.
            /// </summary>
            public void Reset()
            {
                _currentTileIndexPointer = uint.MaxValue;
                _currentTileDataPointer = uint.MaxValue;
                _currentTileCapacity = uint.MaxValue;
                this.LocalId = uint.MaxValue;
            }

            /// <summary>
            /// Moves this enumerator to the given location.
            /// </summary>
            /// <param name="localTileId">The tile id of the location.</param>
            /// <param name="localId">The local id of the location.</param>
            public bool MoveTo(uint localTileId, uint localId)
            {
                var (tileDataPointer, tileIndexPointer, capacity) = _index.FindTile(localTileId);
                if (localId >= capacity)
                { // local id doesn't exist.
                    return false;
                }
                var tile = Tile.FromLocalId(localTileId, _index._zoom);
                
                var (longitude, latitude, hasData) = _index.GetEncodedLocation(tileDataPointer + localId, tile);
                if (!hasData)
                { // no data found at location.
                    return false;
                }

                _currentTile = tile;
                _currentTileCapacity = (uint)capacity;
                _currentTileDataPointer = tileDataPointer;
                _currentTileIndexPointer = tileIndexPointer;
                this.Latitude = latitude;
                this.Longitude = longitude;
                this.LocalId = localId;
                this.LocalTileId = localTileId;
                this.DataPointer = _currentTileDataPointer + localId;

                return true;
            }

            /// <summary>
            /// Move to the next location.
            /// </summary>
            /// <returns>True if there is a location available, false otherwise.</returns>
            public bool MoveNext()
            {
                if (_currentTileIndexPointer == uint.MaxValue)
                { // first move, move to first tile and first pointer.
                    if (_index._tileIndexPointer == 0)
                    { // no data.
                        return false;
                    }

                    // first tile exists, get its data.
                    _currentTileIndexPointer = 0;
                    uint localTileId;
                    (_currentTileDataPointer, localTileId, _currentTileCapacity) = _index.ReadTile(_currentTileIndexPointer);
                    
                    // if the tile is empty, keep moving until a non-empty tile is found.
                    while (_currentTileCapacity <= 0)
                    {
                        _currentTileIndexPointer += 9;
                        if (_currentTileIndexPointer >= _index._tileIndexPointer) return false; // this was the last tile.
                        (_currentTileDataPointer, localTileId, _currentTileCapacity) = _index.ReadTile(_currentTileIndexPointer);
                    }

                    _currentTile = Tile.FromLocalId(localTileId, _index._zoom);
                    this.LocalTileId = localTileId;
                    this.LocalId = 0;
                }
                else
                { // there is a current tile.
                    if (this.LocalId == _currentTileCapacity - 1)
                    { // this was the last location in this tile, move to the next tile.
                        var localTileId = uint.MaxValue;
                        do
                        {
                            _currentTileIndexPointer += 9;
                            if (_currentTileIndexPointer >= _index._tileIndexPointer)
                                return false; // this was the last tile.
                            (_currentTileDataPointer, localTileId, _currentTileCapacity) = _index.ReadTile(_currentTileIndexPointer);
                        } while (_currentTileCapacity <= 0);

                        _currentTile = Tile.FromLocalId(localTileId, _index._zoom);
                        this.LocalTileId = localTileId;
                        this.LocalId = 0;
                    }
                    else
                    {
                        // move to the next location.
                        this.LocalId++;
                    }
                }

                var (longitude, latitude, hasData) = _index.GetEncodedLocation(_currentTileDataPointer + this.LocalId, _currentTile);
                if (!hasData)
                {
                    return this.MoveNext();
                }

                this.Longitude = longitude;
                this.Latitude = latitude;
                this.DataPointer = _currentTileDataPointer;

                return true;
            }
            
            /// <summary>
            /// Gets the data pointer.
            /// </summary>
            public uint DataPointer { get; private set; }
            
            /// <summary>
            /// Gets the tile id.
            /// </summary>
            public uint LocalTileId { get; private set; }
            
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