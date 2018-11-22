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
using Itinero.Transit.Data.Tiles;
using Reminiscence.Arrays;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// A stops database.
    /// </summary>
    public class StopsDb
    {
        // needs to support:
        // - lookup by global id.
        // - lookup by location.

        private readonly TiledLocationIndex _stopLocations; // holds the stop location in a tiled way.
        private readonly int _stopIdHashSize = ushort.MaxValue;
        private readonly ArrayBase<string> _stopIds; // holds the stop id's per stop.

        private const uint NoData = uint.MaxValue;
        private readonly ArrayBase<uint> _stopIdPointersPerHash;
        private uint _stopIdLinkedListPointer = 0;
        private readonly ArrayBase<uint> _stopIdLinkedList;

        /// <summary>
        /// Creates a new stops database.
        /// </summary>
        public StopsDb()
        {
            _stopLocations = new TiledLocationIndex {Moved = this.Move};
            _stopIds = new MemoryArray<string>(0);
            _stopIdPointersPerHash = new MemoryArray<uint>(_stopIdHashSize);
            for (var h = 0; h < _stopIdPointersPerHash.Length; h++)
            {
                _stopIdPointersPerHash[h] = NoData;
            }
            _stopIdLinkedList = new MemoryArray<uint>(0);
        }

        /// <summary>
        /// Called when a block of locations has moved.
        /// </summary>
        /// <param name="from">The from pointer.</param>
        /// <param name="to">The to pointer.</param>
        /// <param name="count">The number of locations that moved.</param>
        private void Move(uint from, uint to, uint count)
        {
            for (var s = 0; s < count; s++)
            {
                _stopIds[to + s] = _stopIds[from + s];
            }
        }

        /// <summary>
        /// Adds a new stop and returns it's internal id.
        /// </summary>
        /// <param name="globalId">The global stop id.</param>
        /// <param name="longitude">The stop longitude.</param>
        /// <param name="latitude">The stop latitude.</param>
        /// <returns>An internal id representing stop.</returns>
        public (uint tileId, uint localId) Add(string globalId, double longitude, double latitude)
        {
            // store location.
            var (localId, tileId, dataPointer) = _stopLocations.Add(longitude, latitude);

            // store stop id at the resulting data pointer.
            while (_stopIds.Length <= dataPointer)
            {
                _stopIds.Resize(_stopIds.Length + 1024);
            }

            _stopIds[dataPointer] = globalId;

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

        private int Hash(string id)
        { // https://stackoverflow.com/questions/5154970/how-do-i-create-a-hashcode-in-net-c-for-a-string-that-is-safe-to-store-in-a
            unchecked
            {
                var hash = 23;
                foreach (var c in id)
                {
                    hash = hash * 31 + c;
                }

                return hash % _stopIdHashSize;
            }
        }
    }
}