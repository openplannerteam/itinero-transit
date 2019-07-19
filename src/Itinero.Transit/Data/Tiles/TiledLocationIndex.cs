using System;
using System.IO;
using System.Runtime.CompilerServices;
using Reminiscence;
using Reminiscence.Arrays;
using Reminiscence.Arrays.Sparse;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]
namespace Itinero.Transit.Data.Tiles
{
    internal partial class TiledLocationIndex
    {
        /// <summary>
        /// The zoom level of this tiled location index.
        /// Same as OSM-zoom levels
        /// </summary>
        public int Zoom { get; }
        
        private const byte DefaultTileCapacityInBytes = 0; 
        private const int CoordinateSizeInBytes = 3; // 3 bytes = 24 bits = 4096 x 4096, the needed resolution depends on the zoom-level, higher, less resolution.
        private const int TileResolutionInBits = CoordinateSizeInBytes * 8 / 2;
        private const int TileSizeInIndex = 9; // 4 bytes for the pointer, 1 for the size.

        private readonly SparseMemoryArray<byte> _tileIndex;
        private uint _tilesCount = 0;
        private readonly ArrayBase<uint> _tiles;
        
        /// <summary>
        /// holds stop locations, encoded relative to the tile they are in.
        /// </summary>
        private readonly ArrayBase<byte> _locations; 
        internal const uint TileNotLoaded = uint.MaxValue;
        private uint _tileDataPointer;
        
        /// <summary>
        /// Creates a new location index.
        /// </summary>
        /// <param name="zoom"></param>
        public TiledLocationIndex(int zoom = 14)
        {
            if (zoom > 19)
            {
                throw new ArgumentException("Use at most 19 as zoom level");
            }
            Zoom = zoom;
            
            _tileIndex = new SparseMemoryArray<byte>(0, emptyDefault: byte.MaxValue);
            _locations = new MemoryArray<byte>(0);
            _tiles = new MemoryArray<uint>(0);
        }

        private TiledLocationIndex(ArrayBase<uint> tiles, SparseMemoryArray<byte> tileIndex, ArrayBase<byte> locations, int zoom, 
            uint tileCount, uint tileDataPointer)
        {
            _tilesCount = tileCount;
            _tiles = tiles;
            _tileIndex = tileIndex;
            _locations = locations;
            Zoom = zoom;
            _tileDataPointer = tileDataPointer;
        }

        /// <summary>
        /// Adds a new latitude-longitude pair.
        /// </summary>
        /// <param name="latitude">The latitude.</param>
        /// <param name="longitude">The longitude</param>
        /// <returns>The tile id, the local id and a data pointer.</returns>
        public (uint tileId, uint localId, uint dataPointer) Add(double longitude, double latitude)
        {
            // get the local tile id.
            var tile = Tile.WorldToTile(longitude, latitude, Zoom);
            var tileId = tile.LocalId;

            // try to find the tile.
            var (tileDataPointer, _, capacity) = GetTile(tileId);
            if (tileDataPointer == TileNotLoaded)
            {
                // create the tile if it doesn't exist yet.
                (tileDataPointer, capacity) = AddTile(tileId);
            }

            // find or create a place to store the location.
            uint nextEmpty;
            
            if (GetEncodedLocation((uint) (tileDataPointer + capacity - 1), tile).hasData)
            {
                // tile is maximum capacity.
                (tileDataPointer, capacity) = IncreaseCapacityForTile(tileId, tileDataPointer);
                nextEmpty = (uint) (tileDataPointer + (capacity / 2));
            }
            else
            {
                // find the last empty slot.
                nextEmpty = (uint) (tileDataPointer + capacity - 1);
                if (nextEmpty > tileDataPointer)
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
        /// Gets the tile for the given tile index.
        /// </summary>
        /// <param name="tileIndex">The index.</param>
        /// <returns>The tile.</returns>
        private uint GetLocalTileId(uint tileIndex)
        {
            if (tileIndex < _tiles.Length)
            {
                return _tiles[tileIndex];
            }

            return TileNotLoaded;
        }
        
        /// <summary>
        /// Finds a tile in the tile index.
        /// </summary>
        /// <param name="localTileId">The local tile id.</param>
        /// <returns>The meta-data about the tile and its data.</returns>
        private (uint tileDataPointer, uint tileIndex, uint capacity) GetTile(uint localTileId)
        {            
            var tilePointerIndex = (long)localTileId * TileSizeInIndex;
            if (tilePointerIndex + TileSizeInIndex >= _tileIndex.Length)
            {
                return (TileNotLoaded, TileNotLoaded, 0);
            }

            if (_tileIndex[tilePointerIndex + 0] == byte.MaxValue &&
                _tileIndex[tilePointerIndex + 1] == byte.MaxValue &&
                _tileIndex[tilePointerIndex + 2] == byte.MaxValue &&
                _tileIndex[tilePointerIndex + 3] == byte.MaxValue &&
                _tileIndex[tilePointerIndex + 4] == byte.MaxValue)
            {
                return (TileNotLoaded, TileNotLoaded, 0);
            }
            
            // find an allocation-less way of doing this:
            //   this is possible it .NET core 2.1 but not netstandard2.0,
            //   we can do this in netstandard2.1 normally.
            //   perhaps implement our own version of bitconverter.
            var tileBytes = new byte[4];
            tileBytes[0] = _tileIndex[tilePointerIndex + 0];
            tileBytes[1] = _tileIndex[tilePointerIndex + 1];
            tileBytes[2] = _tileIndex[tilePointerIndex + 2];
            tileBytes[3] = _tileIndex[tilePointerIndex + 3];
            var tileDataPointer = BitConverter.ToUInt32(tileBytes, 0);
            tileBytes[0] = _tileIndex[tilePointerIndex + 4 + 0];
            tileBytes[1] = _tileIndex[tilePointerIndex + 4 + 1];
            tileBytes[2] = _tileIndex[tilePointerIndex + 4 + 2];
            tileBytes[3] = _tileIndex[tilePointerIndex + 4 + 3];
            var tileIndex = BitConverter.ToUInt32(tileBytes, 0);
            
            return (tileDataPointer, tileIndex, (uint)1 << _tileIndex[tilePointerIndex + 8]);
        }

        /// <summary>
        /// Adds a new tile.
        /// </summary>
        /// <param name="localTileId">The local tile id.</param>
        /// <returns>The meta-data about the tile and its data.</returns>
        private (uint tileDataPointer, uint capacity) AddTile(uint localTileId)
        {
            // store the tile in the index, we need this for enumeration.
            if (_tilesCount >= _tiles.Length)
            {
                var sizeBefore = _tiles.Length;
                _tiles.Resize(_tilesCount + 1024);
                var sizeAfter = _tiles.Length;
                for (var t = sizeBefore; t < sizeAfter; t++)
                {
                    _tiles[t] = TileNotLoaded;
                }
            }
            _tiles[_tilesCount] = localTileId;
            _tilesCount++;
            
            // store the tile pointer.
            var tilePointerIndex = (long)localTileId * TileSizeInIndex;
            if (tilePointerIndex + TileSizeInIndex >= _tileIndex.Length)
            {
                _tileIndex.Resize(tilePointerIndex + TileSizeInIndex + 1024);
            }
            
            // TODO: find an allocation-less way of doing this:
            //   this is possible it .NET core 2.1 but not netstandard2.0,
            //   we can do this in netstandard2.1 normally.
            //   perhaps implement our own version of bitconverter.
            var tileBytes = BitConverter.GetBytes(_tileDataPointer);
            _tileIndex[tilePointerIndex + 0] = tileBytes[0];
            _tileIndex[tilePointerIndex + 1] = tileBytes[1];
            _tileIndex[tilePointerIndex + 2] = tileBytes[2];
            _tileIndex[tilePointerIndex + 3] = tileBytes[3];
            tileBytes = BitConverter.GetBytes(_tilesCount - 1);
            _tileIndex[tilePointerIndex + 4 + 0] = tileBytes[0];
            _tileIndex[tilePointerIndex + 4 + 1] = tileBytes[1];
            _tileIndex[tilePointerIndex + 4 + 2] = tileBytes[2];
            _tileIndex[tilePointerIndex + 4 + 3] = tileBytes[3];
            _tileIndex[tilePointerIndex + 8] = DefaultTileCapacityInBytes;
            
            const int capacity = 1 << DefaultTileCapacityInBytes;
            var pointer = _tileDataPointer;
            _tileDataPointer += capacity;
            return (pointer, capacity);
        }
        
        /// <summary>
        /// Increase capacity for the given tile.
        /// </summary>
        /// <param name="localTileId">The local tile id.</param>
        /// <param name="tileDataPointer">The pointer to the data associated with this tile.</param>
        /// <returns>The new pointer and capacity.</returns>
        private (uint tileDataPointer, uint capacity) IncreaseCapacityForTile(uint localTileId, uint tileDataPointer)
        {
            var tilePointerIndex = (long)localTileId * TileSizeInIndex;
            
            // copy current data, we assume current capacity is at max.
            
            // get current capacity and double it.
            var capacityInBits = _tileIndex[tilePointerIndex + 8];
            _tileIndex[tilePointerIndex + 8] = (byte)(capacityInBits + 1);
            var oldCapacity = 1 << capacityInBits;

            // get the current pointer and update it.
            var newTileDataPointer = _tileDataPointer;
            _tileDataPointer += (uint)(oldCapacity * 2);
            
            // update the tile data pointer in the tile index.
            var pointerBytes = BitConverter.GetBytes(newTileDataPointer); 
            _tileIndex[tilePointerIndex + 0] = pointerBytes[0];
            _tileIndex[tilePointerIndex + 1] = pointerBytes[1];
            _tileIndex[tilePointerIndex + 2] = pointerBytes[2];
            _tileIndex[tilePointerIndex + 3] = pointerBytes[3];
            
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
            Moved?.Invoke(tileDataPointer, newTileDataPointer, (uint)oldCapacity);

            return (newTileDataPointer, (uint)(oldCapacity * 2));
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
        
        
        private (double longitude, double latitude, bool hasData) 
            GetEncodedLocation(uint pointer, Tile tile)
        {
            const int tileResolutionInBits = CoordinateSizeInBytes * 8 / 2;
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
            var y = (int)localCoordinatesEncoded % (1 << tileResolutionInBits);
            var x = (int)localCoordinatesEncoded >> tileResolutionInBits;

            var (longitude, latitude) = tile.FromLocalCoordinates(x, y, 1 << tileResolutionInBits);

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
        /// Returns a deep in-memory-copy.
        /// </summary>
        /// <returns></returns>
        public TiledLocationIndex Clone()
        {
            // it is up to the user to make sure not to clone when writing. 
            var tileIndex = new SparseMemoryArray<byte>(_tileIndex.Length, emptyDefault: byte.MaxValue);
            tileIndex.CopyFrom(_tileIndex, _tileIndex.Length);
            var locations = new MemoryArray<byte>(_locations.Length);
            locations.CopyFrom(_locations, _locations.Length);
            var tiles = new MemoryArray<uint>(_tiles.Length);
            tiles.CopyFrom(_tiles, _tiles.Length);

            return new TiledLocationIndex(tiles, tileIndex, locations, Zoom, _tilesCount, _tileDataPointer);
        }

        internal long WriteTo(Stream stream)
        {
            var length = 0L;
            
            // write version #.
            stream.WriteByte(2);
            length++;
            
            // write zoom.
            stream.WriteByte((byte)Zoom);
            length++;
            
            // write tile index.
            stream.Write(BitConverter.GetBytes(_tilesCount), 0, 4);
            length += 4;
            _tiles.Resize(_tilesCount); // reduce the size, no need to store empty entries.
            length += _tiles.CopyToWithSize(stream);
            
            // write tile pointers.
            length += _tileIndex.CopyToWithHeader(stream);
            
            // write tile data.
            stream.Write(BitConverter.GetBytes(_tileDataPointer), 0, 4);
            length += 4;
            length += _locations.CopyToWithSize(stream);

            return length;
        }

        internal static TiledLocationIndex ReadFrom(Stream stream)
        {
            var buffer = new byte[4];
            
            var version = stream.ReadByte();
            if (version == 1)
            {
                var zoom = stream.ReadByte();

                stream.Read(buffer, 0, 4);
                var tileIndexPointer = BitConverter.ToUInt32(buffer, 0);
                var tileIndex = MemoryArray<byte>.CopyFromWithSize(stream);

                stream.Read(buffer, 0, 4);
                var tileDataPointer = BitConverter.ToUInt32(buffer, 0);
                var locations = MemoryArray<byte>.CopyFromWithSize(stream);
                
                const int OldTileSizeInIndex = 9;
                
                // convert to the new format.
                var tileCount = tileIndexPointer / OldTileSizeInIndex;
                var tiles = new MemoryArray<uint>(tileCount);
                var newTileIndex = new SparseMemoryArray<byte>((tileIndexPointer / OldTileSizeInIndex) * TileSizeInIndex);
                for (var t = 0L; t < tileIndexPointer / OldTileSizeInIndex; t++)
                {
                    var p = t * OldTileSizeInIndex;
                    buffer[0] = tileIndex[p + 0];
                    buffer[1] = tileIndex[p + 1];
                    buffer[2] = tileIndex[p + 2];
                    buffer[3] = tileIndex[p + 3];
                    var tileId = BitConverter.ToUInt32(buffer, 0);
                    tiles[t] = tileId;

                    var tilePointer = (long)tileId * TileSizeInIndex;
                    newTileIndex[tilePointer + 0] =  tileIndex[p + 4 + 0];
                    newTileIndex[tilePointer + 1] =  tileIndex[p + 4 + 1];
                    newTileIndex[tilePointer + 2] =  tileIndex[p + 4 + 2];
                    newTileIndex[tilePointer + 3] =  tileIndex[p + 4 + 3];
                    newTileIndex[tilePointer + 4] =  tileIndex[p + 4 + 4];
                }

                return new TiledLocationIndex(tiles, newTileIndex, locations, zoom, tileCount, tileDataPointer);
            }
            else if (version == 2)
            {
                var zoom = stream.ReadByte();
                
                stream.Read(buffer, 0, 4);
                var tilesCount = BitConverter.ToUInt32(buffer, 0);
                
                var tiles = MemoryArray<uint>.CopyFromWithSize(stream);

                var tileIndex = SparseMemoryArray<byte>.CopyFromWithHeader(stream);

                stream.Read(buffer, 0, 4);
                var tileDataPointer = BitConverter.ToUInt32(buffer, 0);
                var locations = MemoryArray<byte>.CopyFromWithSize(stream);
                
                return new TiledLocationIndex(tiles, tileIndex, locations, zoom, tilesCount, tileDataPointer);
            }
            else
            {
                throw new InvalidDataException($"Cannot read {nameof(TiledLocationIndex)}, invalid version #.");
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
    }
}