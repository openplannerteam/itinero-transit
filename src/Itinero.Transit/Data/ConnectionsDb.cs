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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using Reminiscence.Arrays;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]
namespace Itinero.Transit.Data
{
    /// <summary>
    /// A connections database.
    /// </summary>
    public class ConnectionsDb
    {
        // this is a connections database, it needs to support:
        // -> adding/removing connections by their global id.
        // -> an always sorted version by departure time.
        // -> an always sorted version by arrival time.
        
        // a connection can be queried by:
        // - a stable global id stored in a dictionary, this is a string.
        // - an id for internal usage, this can change when the connection is updated.
        // - by enumerating them sorted by either:
        //  -> departure time.
        //  -> arrival time.
        
        // a connection doesn't have:
        // - delay information, just add this to the departure time. the delay offset information is just 
        //   meta data and we can store it as such.

        // this stores the connections data:
        // - stop1 (8bytes): the departure stop id.
        // - stop2 (8bytes): the arrival stop.
        // - departure time (4bytes): seconds since 1970-1-1: 4bytes.
        // - travel time in seconds (2bytes): the travel time in seconds, max 65535.
        private uint _nextInternalId; // the next empty position in the connection data array, divided by the connection size in bytes.
        private readonly ArrayBase<byte> _data; // the connection data.
        
        // this stores the connections global id index.
        private readonly int _globalIdHashSize = ushort.MaxValue;
        private readonly ArrayBase<uint> _globalIdPointersPerHash;
        private uint _globalIdLinkedListPointer = 0;
        private readonly ArrayBase<uint> _globalIdLinkedList;

        // the connections meta-data, its global, trip.
        private readonly ArrayBase<string> _globalIds; // holds the global ids.
        private readonly ArrayBase<uint> _tripIds; // holds the trip ids.

        private readonly ArrayBase<uint> _departureWindowPointers; // pointers to where the connection window blocks are stored.
        private readonly ArrayBase<uint> _departurePointers; // pointers to the connections sorted by departure time per window block.

        private readonly ArrayBase<uint> _arrivalWindowPointers; // pointers to where the connection window blocks are stored.
        private readonly ArrayBase<uint> _arrivalPointers; // pointers to the connections sorted by arrival time per window block.
            
        private const uint NoData = uint.MaxValue;
        private readonly long _windowSizeInSeconds = 60; // one window per minute.
        private const int ConnectionSizeInBytes = 8 + 8 + 4 + 2;
        
        /// <summary>
        /// Creates a new connections db.
        /// </summary>
        public ConnectionsDb(int windowSizeInSeconds = 60)
        {
            _windowSizeInSeconds = windowSizeInSeconds;
            
            // initialize the data array.
            _data = new MemoryArray<byte>(0);
            _nextInternalId = 0;
            
            // initialize the meta-data arrays.
            _globalIds = new MemoryArray<string>(0);
            _tripIds = new MemoryArray<uint>(0);
            _nextInternalId = 0;
            
            // initialize the ids reverse index.
            _globalIdPointersPerHash = new MemoryArray<uint>(_globalIdHashSize);
            for (var h = 0; h < _globalIdPointersPerHash.Length; h++)
            {
                _globalIdPointersPerHash[h] = NoData;
            }
            _globalIdLinkedList = new MemoryArray<uint>(0);
        }
        
        /// <summary>
        /// Adds a new connection.
        /// </summary>
        /// <param name="stop1">The first stop.</param>
        /// <param name="stop2">The last stop.</param>
        /// <param name="globalId">The global id.</param>
        /// <param name="departureTime">The departure time.</param>
        /// <param name="travelTime">The travel time in seconds.</param>
        /// <param name="tripId">The trip id.</param>
        /// <returns>An internal id representing the connection in this transit db.</returns>
        public uint Add((uint localTileId, uint localId) stop1,
            (uint localTileId, uint localId) stop2, string globalId, DateTime departureTime, ushort travelTime, uint tripId)
        {
            // get the next internal id.
            var internalId = _nextInternalId;
            _nextInternalId++;
            
            // set this connection info int the data array.
            var departureSeconds = (uint)departureTime.ToUnixTime();
            SetConnection(internalId, stop1, stop2, departureSeconds, travelTime);
            
            // set trip and global ids.
            SetTrip(internalId, tripId);
            SetGlobalId(internalId, globalId);
            
            // update departure time index.
            
            // update arrival time index.

            return internalId;
        }
        
        private void SetConnection(uint internalId, (uint localTileId, uint localId) stop1, (uint localTileId, uint localId) stop2, 
            uint departure, ushort travelTime)
        {
            // make sure the data array is big enough.
            var dataPointer = internalId * ConnectionSizeInBytes;
            while (_data.Length <= dataPointer)
            {
                var oldLength = _data.Length;
                _data.Resize(_data.Length + 1024);
                for (var i = oldLength; i < _data.Length; i++)
                {
                    _data[i] = byte.MaxValue;
                }
            }
            
            var offset = 0;
            var bytes = BitConverter.GetBytes(stop1.localTileId);
            for (var b = 0; b < 4; b++)
            {
                _data[dataPointer + offset + b] = bytes[b];
            }
            offset += 4;
            bytes = BitConverter.GetBytes(stop1.localId);
            for (var b = 0; b < 4; b++)
            {
                _data[dataPointer + offset + b] = bytes[b];
            }
            offset += 4;
            bytes = BitConverter.GetBytes(stop2.localTileId);
            for (var b = 0; b < 4; b++)
            {
                _data[dataPointer + offset + b] = bytes[b];
            }
            offset += 4;
            bytes = BitConverter.GetBytes(stop2.localId);
            for (var b = 0; b < 4; b++)
            {
                _data[dataPointer + offset + b] = bytes[b];
            }
            offset += 4;
            bytes = BitConverter.GetBytes(departure);
            for (var b = 0; b < 2; b++)
            {
                _data[dataPointer + offset + b] = bytes[b];
            }
            offset += 4;
            bytes = BitConverter.GetBytes(travelTime);
            for (var b = 0; b < 2; b++)
            {
                _data[dataPointer + offset + b] = bytes[b];
            }
            offset += 2;
        }

        private ((uint localTileId, uint localId) stop1, (uint localTileId, uint localId) stop2,
            uint departure, ushort travelTime) GetConnection(uint internalId)
        {
            var dataPointer = internalId * ConnectionSizeInBytes;
            if (_data.Length <= dataPointer + ConnectionSizeInBytes)
            {
                return ((uint.MaxValue, uint.MaxValue), (uint.MaxValue, uint.MaxValue), uint.MaxValue, ushort.MaxValue);
            }

            var bytes = new byte[ConnectionSizeInBytes];
            for (var b = 0; b < ConnectionSizeInBytes; b++)
            {
                bytes[b] = _data[dataPointer + b];
            }
            
            var offset = 0;
            var stop1 = (BitConverter.ToUInt32(bytes, 0),
                BitConverter.ToUInt32(bytes, 4));
            if (stop1.Item1 == uint.MaxValue &&
                stop1.Item1 == uint.MaxValue)
            {
                return ((uint.MaxValue, uint.MaxValue), (uint.MaxValue, uint.MaxValue), uint.MaxValue, ushort.MaxValue);
            }
            offset += 8;
            
            var stop2 = (BitConverter.ToUInt32(bytes, offset + 0),
                BitConverter.ToUInt32(bytes, offset + 4));
            offset += 8;
            
            var departureTime = BitConverter.ToUInt32(bytes, offset);
            offset += 4;
            
            var travelTime = BitConverter.ToUInt16(bytes, offset);
            offset += 2;
            
            return (stop1, stop2, departureTime, travelTime);
        }

        private void SetTrip(uint internalId, uint tripId)
        {
            while (_tripIds.Length <= internalId)
            {
                _tripIds.Resize(_tripIds.Length + 1024);
            }

            _tripIds[internalId] = tripId;
        }

        private void SetGlobalId(uint internalId, string globalId)
        {
            while (_globalIds.Length <= internalId)
            {
                _globalIds.Resize(_globalIds.Length + 1024);
            }

            _globalIds[internalId] = globalId;

            // add stop id to the index.
            _globalIdLinkedListPointer += 2;
            while (_globalIdLinkedList.Length <= _globalIdLinkedListPointer)
            {
                _globalIdLinkedList.Resize(_globalIdLinkedList.Length + 1024);
            }

            var hash = Hash(globalId);
            _globalIdLinkedList[_globalIdLinkedListPointer - 2] = internalId;
            _globalIdLinkedList[_globalIdLinkedListPointer - 1] = _globalIdPointersPerHash[hash];
            _globalIdPointersPerHash[hash] = _globalIdLinkedListPointer - 2;
        }
        
        private int Hash(string id)
        { // https://stackoverflow.com/questions/5154970/how-do-i-create-a-hashcode-in-net-c-for-a-string-that-is-safe-to-store-in-a
            unchecked
            {
                var hash = 23;
                foreach (var c in id)
                {
                    hash = hash * 31 + c;
                }

                return hash % _globalIdHashSize;
            }
        }

        private ((uint localTileId, uint localId) stop1, (uint localTileId, uint localId) stop2, 
            ushort departureDay, byte windowOffset, ushort travelTime, uint tripId, bool hasData) ReadConnection(uint dataPointer)
        {
            if (_data.Length <= dataPointer + ConnectionSizeInBytes)
            {
                return ((uint.MaxValue, uint.MaxValue), (uint.MaxValue, uint.MaxValue), ushort.MaxValue,
                    byte.MaxValue, ushort.MaxValue, uint.MaxValue, false);
            }

            var bytes = new byte[ConnectionSizeInBytes];
            for (var b = 0; b < ConnectionSizeInBytes; b++)
            {
                bytes[b] = _data[dataPointer + b];
            }

            var offset = 0;
            var stop1 = (BitConverter.ToUInt32(bytes, 0),
                BitConverter.ToUInt32(bytes, 4));
            if (stop1.Item1 == uint.MaxValue &&
                stop1.Item1 == uint.MaxValue)
            {
                return ((uint.MaxValue, uint.MaxValue), (uint.MaxValue, uint.MaxValue), ushort.MaxValue,
                    byte.MaxValue, ushort.MaxValue, uint.MaxValue, false);
            }
            offset += 8;
            
            var stop2 = (BitConverter.ToUInt32(bytes, offset + 0),
                BitConverter.ToUInt32(bytes, offset + 4));
            offset += 8;
            
            var departureDay = BitConverter.ToUInt16(bytes, offset);
            offset += 2;
            
            var windowOffset = bytes[offset];
            offset += 2;
            
            var travelTime = BitConverter.ToUInt16(bytes, offset);
            offset += 2;

            var internalId = BitConverter.ToUInt32(bytes, offset);
            offset += 4;
            return (stop1, stop2, departureDay, windowOffset, travelTime, internalId, true);
        }

        private void CopyConnection(uint dataPointerFrom, uint dataPointerTo)
        {
            for (var p = 0; p < ConnectionSizeInBytes; p++)
            {
                _data[dataPointerTo + p] = _data[dataPointerFrom + p];
            }
        }

        private uint CopyAndExpandBlock(uint windowPointer, uint capacity)
        {
            var newWindowPointer = _nextInternalId;
            _nextInternalId += capacity * 2;
            
            while (_data.Length <= _nextInternalId * ConnectionSizeInBytes)
            {
                var oldLength = _data.Length;
                _data.Resize(_data.Length + 1024);
                for (var i = oldLength; i < _data.Length; i++)
                {
                    _data[i] = byte.MaxValue;
                }
            }

            for (uint c = 0; c < capacity; c++)
            {
                CopyConnection(windowPointer + c, newWindowPointer + c);
            }

            return newWindowPointer;
        }

        /// <summary>
        /// Gets a reader.
        /// </summary>
        /// <returns></returns>
        internal ConnectionsDbReader GetReader()
        {
            return new ConnectionsDbReader(this);
        }
        
        /// <summary>
        /// A connections db reader.
        /// </summary>
        internal class ConnectionsDbReader
        {
            private readonly ConnectionsDb _db;

            internal ConnectionsDbReader(ConnectionsDb db)
            {
                _db = db;
            }

            private uint _internalId;
            private (uint localTileId, uint localId) _stop1;
            private (uint localTileId, uint localId) _stop2;
            private uint _departureTime;
            private ushort _travelTime;
            
            
            
            /// <summary>
            /// Gets the global id.
            /// </summary>
            public string GlobalId => _db._globalIds[_internalId];
            
            /// <summary>
            /// Gets the trip id.
            /// </summary>
            public uint TripId => _db._tripIds[_internalId];

            /// <summary>
            /// Moves this reader to the connection with the given internal id.
            /// </summary>
            /// <param name="internalId">The internal id.</param>
            /// <returns>True if the connection was found and there is data.</returns>
            public bool MoveTo(uint internalId)
            {
                var details = _db.GetConnection(internalId);
                if (details.stop1.localTileId == uint.MaxValue)
                { // no data.
                    return false;
                }

                _internalId = internalId;
                _stop1 = details.stop1;
                _stop2 = details.stop2;
                _departureTime = details.departure;
                _travelTime = details.travelTime;
                
                return true;
            }

            /// <summary>
            /// Moves this reader to the connection with the given global id.
            /// </summary>
            /// <param name="globalId">The global id.</param>
            /// <returns>True if the connection was found and there is data.</returns>
            public bool MoveTo(string globalId)
            {
                var hash = _db.Hash(globalId);
                var pointer = _db._globalIdPointersPerHash[hash];
                while (pointer != NoData)
                {
                    var internalId = _db._globalIdLinkedList[pointer + 0];

                    if (this.MoveTo(internalId))
                    {
                        var potentialMatch = this.GlobalId;
                        if (potentialMatch == globalId)
                        {
                            return true;
                        }
                    }

                    pointer = _db._globalIdLinkedList[pointer + 2];
                }

                return false;
            }
        }
    }
}