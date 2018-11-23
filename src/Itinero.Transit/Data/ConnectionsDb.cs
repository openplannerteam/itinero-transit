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
using System.Security.Cryptography.X509Certificates;
using Reminiscence.Arrays;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// A connections database.
    /// </summary>
    public class ConnectionsDb
    {
        // this is a connections database, it needs to support:
        // -> adding/removing connections.
        
        // a connection is:
        // - stop1 (8bytes): the departure stop id.
        // - stop2 (8bytes): the arrival stop.
        // - departure time:
        //   -> the date part, days since 1970-1-1: 2bytes.
        //   -> the seconds offset in the window: 1byte.
        // - travel time in seconds (2bytes): the travel time in seconds, max 65535.
        // - trip id (4bytes): the trip id.
        
        // a connection can be queried by:
        // - a stable global id store in a dictionary, this is a string.
        // - an id for internal usage, this can change when the connection is updated.
        // - by enumerating them sorted by either:
        //  -> departure time.
        //  -> arrival time.
        
        // a connection doesn't have:
        // - delay information, just add this to the departure time. the delay information is just 
        //   meta data and we can store it as such.

        private const uint NoData = uint.MaxValue;
        private readonly ArrayBase<uint> _windowPointers; // pointers to where the connection windows blocks are stored.
        private readonly long _windowSizeInSeconds = 60; // one window per minute.
        private readonly ArrayBase<byte> _data; // the actual connection data.
        private const int ConnectionSizeInBytes = 8 + 8 + 2 + 1 + 2 + 2 + 4;
        private uint _dataPointer; // the next empty position in the connection data array, divided by the connection size in bytes.

        /// <summary>
        /// Creates a new connections db.
        /// </summary>
        public ConnectionsDb(int windowSizeInSeconds = 60)
        {
            _windowSizeInSeconds = windowSizeInSeconds;
            _windowPointers = new MemoryArray<uint>((long)System.Math.Ceiling(24d * 60 * 60 / _windowSizeInSeconds) * 2);
            for (var w = 0; w < _windowPointers.Length / 2; w++)
            {
                _windowPointers[w * 2 + 0] = NoData; // point to nothing.
                _windowPointers[w * 2 + 1] = 0; // empty.
            }
            _data = new MemoryArray<byte>(0);
            _dataPointer = 0;
        }
        
        /// <summary>
        /// Adds a new connection.
        /// </summary>
        /// <param name="stop1">The first stop.</param>
        /// <param name="stop2">The last stop.</param>
        /// <param name="departureTime">The departure time.</param>
        /// <param name="travelTime"></param>
        /// <param name="tripId"></param>
        /// <returns></returns>
        public (uint window, uint localId) Add((uint localTileId, uint localId) stop1,
            (uint localTileId, uint localId) stop2, DateTime departureTime, ushort travelTime, uint tripId)
        {
            // determine the window to store this connection into.
            var secondsSinceMidnight = departureTime.TimeOfDay.TotalSeconds;
            var window = (int)System.Math.Floor(secondsSinceMidnight / _windowSizeInSeconds);
            
            // compose connection details.
            var windowOffset = (byte)(secondsSinceMidnight - (window * _windowSizeInSeconds));
            var departureDay = (ushort)departureTime.ToUnixDay();
            
            // add connection to the window block or create a new one.
            if (_windowPointers[window * 2 + 0] == NoData)
            { // create a new block.
                _windowPointers[window * 2 + 0] = _dataPointer;
                _windowPointers[window * 2 + 1] = 1;
                WriteConnection(_dataPointer, stop1, stop2, departureDay, windowOffset, travelTime, tripId);
                _dataPointer += 1;

                return ((uint)window, 0);
            }
            
            // check if there is data at the last entry of if capacity is zero.
            var windowPointer = _windowPointers[window * 2 + 0];
            var capacity = _windowPointers[window * 2 + 1];
            var nextEmpty = windowPointer + capacity - 1;
            if (capacity == 0 ||
                ReadConnection(nextEmpty).hasData)
            { // block is max capacity, increase it.
                windowPointer = CopyAndExpandBlock(windowPointer, capacity);
                nextEmpty = windowPointer + capacity;
                capacity = capacity * 2;
                _windowPointers[window * 2 + 0] = windowPointer;
                _windowPointers[window * 2 + 1] = capacity;
            }
            else
            { // find the first empty block.
                for (var p = nextEmpty - 1; p >= windowPointer; p--)
                {
                    if (ReadConnection(p).hasData)
                    {
                        break;
                    }

                    nextEmpty--;
                }
            }
            
            // write the connection.
            WriteConnection(nextEmpty, stop1, stop2, departureDay, windowOffset, travelTime, tripId);

            return ((uint)window, nextEmpty - windowPointer);
        }

        private void WriteConnection(uint dataPointer, (uint localTileId, uint localId) stop1, (uint localTileId, uint localId) stop2, 
            ushort departureDay, byte windowOffset, ushort travelTime, uint tripId)
        {
            while (_data.Length <= dataPointer * ConnectionSizeInBytes)
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
            bytes = BitConverter.GetBytes(departureDay);
            for (var b = 0; b < 2; b++)
            {
                _data[dataPointer + offset + b] = bytes[b];
            }
            offset += 2;
            _data[dataPointer + offset] = windowOffset;
            offset += 1;
            bytes = BitConverter.GetBytes(travelTime);
            for (var b = 0; b < 2; b++)
            {
                _data[dataPointer + offset + b] = bytes[b];
            }
            offset += 2;
            bytes = BitConverter.GetBytes(tripId);
            for (var b = 0; b < 4; b++)
            {
                _data[dataPointer + offset + b] = bytes[b];
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

            var tripId = BitConverter.ToUInt32(bytes, offset);
            offset += 4;
            return (stop1, stop2, departureDay, windowOffset, travelTime, tripId, true);
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
            var newWindowPointer = _dataPointer;
            _dataPointer += capacity * 2;
            
            while (_data.Length <= _dataPointer * ConnectionSizeInBytes)
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
    }
}