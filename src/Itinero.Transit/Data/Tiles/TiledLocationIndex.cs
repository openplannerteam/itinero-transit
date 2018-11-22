using System;
using Reminiscence.Arrays;

namespace Itinero.Transit.Data.Tiles
{
    public class TiledLocationIndex
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
        public (uint localId, uint localTileId, uint dataPointer) Add(double latitude, double longitude)
        {
            // get the local tile id.
            var tile = Tile.WorldToTile(longitude, latitude, _zoom);
            var localTileId = tile.LocalId;

            // try to find the tile.
            var (tileDataPointer, tileIndexPointer, capacity) = FindTile(localTileId);
            if (tileDataPointer == TileNotLoaded)
            { // create the tile if it doesn't exist yet.
                (tileDataPointer, tileIndexPointer, capacity) = AddTile(localTileId);
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
                for (var p = nextEmpty - 1; p >= tileDataPointer; p--)
                {
                    if (!GetEncodedLocation(p, tile).hasData)
                    { // non-empty slot found.
                        break;
                    }
                    nextEmpty = p;
                }
            }
            
            var localId = (nextEmpty - tileDataPointer);
            
            // set the vertex data.
            SetEncodedLocation(nextEmpty, tile, longitude, latitude);

            return (localId, localTileId, nextEmpty);
        }

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
            var length = _locations.Length / CoordinateSizeInBytes;
            while (_tileDataPointer + (oldCapacity * 2) >= length)
            {
                length += 1024;
            }
            if (length >= _locations.Length / CoordinateSizeInBytes)
            {
                _locations.Resize(length * CoordinateSizeInBytes);
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
            var vertexPointer1 = pointer1 * CoordinateSizeInBytes;
            var vertexPointer2 = pointer2 * CoordinateSizeInBytes;

            for (var b = 0; b < CoordinateSizeInBytes; b++)
            {
                _locations[vertexPointer2 + b] = _locations[vertexPointer1 + b];
            }
        }
        
        
        private (double longitude, double latitude, bool hasData) GetEncodedLocation(uint pointer, Tile tile)
        {
            const int TileResolutionInBits = CoordinateSizeInBytes * 8 / 2;
            var stopPointer = pointer * (long)CoordinateSizeInBytes;

            var bytes = new byte[4];
            for (var b = 0; b < CoordinateSizeInBytes; b++)
            {
                bytes[b] = _locations[stopPointer + b];
            }

            var localCoordinatesEncoded = BitConverter.ToInt32(bytes, 0);
            if (localCoordinatesEncoded == uint.MaxValue)
            {
                return (0, 0, false);
            }
            var y = localCoordinatesEncoded % (1 << TileResolutionInBits);
            var x = localCoordinatesEncoded >> TileResolutionInBits;

            var (longitude, latitude) = tile.FromLocalCoordinates(x, y, 1 << TileResolutionInBits);

            return (longitude, latitude, true);
        }
        
        private void SetEncodedLocation(uint pointer, Tile tile, double longitude, double latitude)
        {
            var localCoordinates = tile.ToLocalCoordinates(longitude, latitude, 1 << TileResolutionInBits);
            var localCoordinatesEncoded = (localCoordinates.x << TileResolutionInBits) + localCoordinates.y;
            var localCoordinatesBits = BitConverter.GetBytes(localCoordinatesEncoded);
            var vertexPointer = pointer * (long)CoordinateSizeInBytes;
            for (var b = 0; b < CoordinateSizeInBytes; b++)
            {
                _locations[vertexPointer + b] = localCoordinatesBits[b];
            }
        }
    }
}