using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.Data.Tiles;
using Reminiscence;
using Reminiscence.Arrays;
using Attribute = Itinero.Transit.Data.Attributes.Attribute;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]
namespace Itinero.Transit.Data
{
    /// <summary>
    /// A stops database.
    /// </summary>
    public class StopsDb
    {
        private readonly TiledLocationIndex _stopLocations; // holds the stop location in a tiled way.
        private readonly int _stopIdHashSize = ushort.MaxValue;
        private readonly ArrayBase<string> _stopIds; // holds the stop ids per stop.
        private readonly ArrayBase<uint> _stopAttributeIds; // holds the stop attribute ids per stop.

        private const uint NoData = uint.MaxValue;
        private readonly ArrayBase<uint> _stopIdPointersPerHash;
        private uint _stopIdLinkedListPointer = 0;
        private readonly ArrayBase<uint> _stopIdLinkedList;
        
        private readonly AttributesIndex _attributes;

        /// <summary>
        /// Creates a new stops database.
        /// </summary>
        internal StopsDb()
        {
            _stopLocations = new TiledLocationIndex {Moved = this.Move};
            _stopIds = new MemoryArray<string>(0);
            _stopAttributeIds = new MemoryArray<uint>(0);
            _stopIdPointersPerHash = new MemoryArray<uint>(_stopIdHashSize);
            for (var h = 0; h < _stopIdPointersPerHash.Length; h++)
            {
                _stopIdPointersPerHash[h] = NoData;
            }
            _stopIdLinkedList = new MemoryArray<uint>(0);
            _attributes = new AttributesIndex(AttributesIndexMode.ReverseStringIndexKeysOnly);
        }

        private StopsDb(TiledLocationIndex stopLocations, ArrayBase<string> stopIds, ArrayBase<uint> stopAttributeIds,
            ArrayBase<uint> stopIdPointsPerHash, ArrayBase<uint> stopIdLinkedList, AttributesIndex attributes,
                uint stopIdLinkedListPointer)
        {
            _stopIdLinkedListPointer = stopIdLinkedListPointer;
            _stopLocations = stopLocations;
            _stopIds = stopIds;
            _stopAttributeIds = stopAttributeIds;
            _stopIdPointersPerHash = stopIdPointsPerHash;
            _stopIdLinkedList = stopIdLinkedList;
            _attributes = attributes;
        }

        /// <summary>
        /// Called when a block of locations has moved.
        /// </summary>
        /// <param name="from">The from pointer.</param>
        /// <param name="to">The to pointer.</param>
        /// <param name="count">The number of locations that moved.</param>
        private void Move(uint from, uint to, uint count)
        {
            while (_stopIds.Length <= to + count)
            {
                _stopIds.Resize(_stopIds.Length + 1024);
                _stopAttributeIds.Resize(_stopAttributeIds.Length + 1024);
            }
            for (var s = 0; s < count; s++)
            {
                _stopIds[to + s] = _stopIds[from + s];
                _stopAttributeIds[to + s] = _stopAttributeIds[from + s];
            }
        }

        /// <summary>
        /// Adds a new stop and returns it's internal id.
        /// </summary>
        /// <param name="globalId">The global stop id.</param>
        /// <param name="longitude">The stop longitude.</param>
        /// <param name="latitude">The stop latitude.</param>
        /// <param name="attributes">The stop attributes.</param>
        /// <returns>An internal id representing the stop in this transit db.</returns>
        internal (uint tileId, uint localId) Add(string globalId, double longitude, double latitude, IEnumerable<Attribute> attributes = null)
        {
            // store location.
            var (tileId, localId, dataPointer) = _stopLocations.Add(longitude, latitude);

            // store stop id at the resulting data pointer.
            while (_stopIds.Length <= dataPointer)
            {
                _stopIds.Resize(_stopIds.Length + 1024);
                _stopAttributeIds.Resize(_stopIds.Length + 1024);
            }
            _stopIds[dataPointer] = globalId;
            _stopAttributeIds[dataPointer] = _attributes.Add(attributes);

            // add stop id to the index.
            _stopIdLinkedListPointer += 3;
            while (_stopIdLinkedList.Length <= _stopIdLinkedListPointer)
            {
                _stopIdLinkedList.Resize(_stopIdLinkedList.Length + 1024);
            }

            var hash = Hash(globalId);
            _stopIdLinkedList[_stopIdLinkedListPointer - 3] = tileId;
            _stopIdLinkedList[_stopIdLinkedListPointer - 2] = localId;
            _stopIdLinkedList[_stopIdLinkedListPointer - 1] = _stopIdPointersPerHash[hash];
            _stopIdPointersPerHash[hash] = _stopIdLinkedListPointer - 3;

            return (tileId, localId);
        }

        /// <summary>
        /// Gets the stop locations index.
        /// </summary>
        internal TiledLocationIndex StopLocations => _stopLocations;

        private uint Hash(string id)
        { // https://stackoverflow.com/questions/5154970/how-do-i-create-a-hashcode-in-net-c-for-a-string-that-is-safe-to-store-in-a
            unchecked
            {
                var hash = (uint) 23;
                foreach (var c in id)
                {
                    hash = hash * 31 + c;
                }

                return  (uint) (hash % _stopIdHashSize);
            }
        }

        internal long WriteTo(Stream stream)
        {
            var length = 0L;
            
            // write version #.
            stream.WriteByte(1);
            length++;
                
            // write location index.
            length += _stopLocations.WriteTo(stream);
            
            // write data.
            length += _stopIds.CopyToWithSize(stream);
            length += _stopAttributeIds.CopyToWithSize(stream);
            length += _stopIdPointersPerHash.CopyToWithSize(stream);
            var bytes = BitConverter.GetBytes(_stopIdLinkedListPointer);
            length += bytes.Length;
            stream.Write(bytes, 0, 4);
            length += _stopIdLinkedList.CopyToWithSize(stream);
            
            // write attributes.
            length += _attributes.Serialize(stream);

            return length;
        }

        internal static StopsDb ReadFrom(Stream stream)
        {
            var buffer = new byte[4];
            
            var version = stream.ReadByte();
            if (version != 1) throw new InvalidDataException($"Cannot read {nameof(StopsDb)}, invalid version #.");
            
            // read location index.
            var stopLocations = TiledLocationIndex.ReadFrom(stream);
            
            // read data.
            var stopIds = MemoryArray<string>.CopyFromWithSize(stream);
            var stopAttributeIds = MemoryArray<uint>.CopyFromWithSize(stream);
            var stopIdPointsPerHash = MemoryArray<uint>.CopyFromWithSize(stream);
            stream.Read(buffer, 0, 4);
            var stopIdLinkedListPointer = BitConverter.ToUInt32(buffer, 0);
            var stopIdLinkedList = MemoryArray<uint>.CopyFromWithSize(stream);
            
            // read attributes.
            var attributes = AttributesIndex.Deserialize(stream);
            
            return new StopsDb(stopLocations, stopIds, stopAttributeIds, stopIdPointsPerHash, stopIdLinkedList,
                attributes, stopIdLinkedListPointer);
        }

        /// <summary>
        /// Returns a deep in-memory copy.
        /// </summary>
        /// <returns></returns>
        public StopsDb Clone()
        {
            // it is up to the user to make sure not to clone when writing. 
            
            var stopLocations = _stopLocations.Clone();
            
            var stopIds = new MemoryArray<string>(_stopIds.Length);
            stopIds.CopyFrom(_stopIds, _stopIds.Length);
            var stopAttributesIds = new MemoryArray<uint>(_stopAttributeIds.Length);
            stopAttributesIds.CopyFrom(_stopAttributeIds, _stopAttributeIds.Length);
            var stopIdPointersPerHash = new MemoryArray<uint>(_stopIdPointersPerHash.Length);
            stopIdPointersPerHash.CopyFrom(_stopIdPointersPerHash, _stopIdPointersPerHash.Length);
            var stopIdLinkedList = new MemoryArray<uint>(_stopIdLinkedList.Length);
            stopIdLinkedList.CopyFrom(_stopIdLinkedList, _stopIdLinkedList.Length);
            
            // don't clone the attributes, it's supposed to be add-only anyway.
            // it's up to the user not to write to it from multiple threads.
            return new StopsDb(stopLocations, stopIds, stopAttributesIds, stopIdPointersPerHash, stopIdLinkedList, _attributes,
                _stopIdLinkedListPointer);
        }

        /// <summary>
        /// Gets a reader.
        /// </summary>
        /// <returns>A reader.</returns>
        public StopsDbReader GetReader()
        {
            return new StopsDbReader(this);
        }

        /// <summary>
        /// A stops reader.
        /// </summary>
        public class StopsDbReader : IStop
        {
            private readonly StopsDb _stopsDb;
            private readonly TiledLocationIndex.Enumerator _locationEnumerator;

            internal StopsDbReader(StopsDb stopsDb)
            {
                _stopsDb = stopsDb;

                _locationEnumerator = _stopsDb._stopLocations.GetEnumerator();
            }

            /// <summary>
            /// Resets this enumerator.
            /// </summary>
            // ReSharper disable once UnusedMember.Global
            public void Reset()
            {
                _locationEnumerator.Reset();
            }

            /// <summary>
            /// Moves this enumerator to the given stop.
            /// </summary>
            /// <param name="localTileId">The local tile id.</param>
            /// <param name="localId">The local id.</param>
            /// <returns>True if there is more data.</returns>
            public bool MoveTo(uint localTileId, uint localId)
            {
                return _locationEnumerator.MoveTo(localTileId, localId);
            }

            /// <summary>
            /// Moves this enumerator to the given stop.
            /// </summary>
            /// <param name="stop">The stop.</param>
            /// <returns>True if there is more data.</returns>
            public bool MoveTo((uint localTileId, uint localId) stop)
            {
                return _locationEnumerator.MoveTo(stop.localTileId, stop.localId);
            }

            /// <summary>
            /// Moves this enumerator to the stop with the given global id.
            /// </summary>
            /// <param name="globalId">The global id.</param>
            /// <returns>True if the stop was found and there is data.</returns>
            public bool MoveTo(string globalId)
            {
                var hash = _stopsDb.Hash(globalId);
                var pointer = _stopsDb._stopIdPointersPerHash[hash];
                while (pointer != NoData)
                {
                    var localTileId = _stopsDb._stopIdLinkedList[pointer + 0];
                    var localId = _stopsDb._stopIdLinkedList[pointer + 1];

                    if (this.MoveTo(localTileId, localId))
                    {
                        var potentialMatch = this.GlobalId;
                        if (potentialMatch == globalId)
                        {
                            return true;
                        }
                    }

                    pointer = _stopsDb._stopIdLinkedList[pointer + 2];
                }

                return false;
            }
            
            /// <summary>
            /// Moves to the next stop.
            /// </summary>
            /// <returns>True if there is more data.</returns>
            public bool MoveNext()
            {
                return _locationEnumerator.MoveNext();
            }

            /// <summary>
            /// Gets the global id.
            /// </summary>
            public string GlobalId => _stopsDb._stopIds[_locationEnumerator.DataPointer];

            /// <summary>
            /// Gets the stop id.
            /// </summary>
            public (uint tileId, uint localId) Id =>
                (_locationEnumerator.TileId, _locationEnumerator.LocalId);

            /// <summary>
            /// Gets the latitude.
            /// </summary>
            public double Latitude => _locationEnumerator.Latitude;

            /// <summary>
            /// Gets the longitude.
            /// </summary>
            public double Longitude => _locationEnumerator.Longitude;

            /// <summary>
            /// Gets the attributes.
            /// </summary>
            public IAttributeCollection Attributes => _stopsDb._attributes.Get(_stopsDb._stopAttributeIds[_locationEnumerator.DataPointer]);
        }
    }
}