using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.CompilerServices;
using Itinero.Transit.Algorithms.Sorting;
using Reminiscence;
using Reminiscence.Arrays;

// ReSharper disable RedundantAssignment

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Functional")]

namespace Itinero.Transit.Data
{
    public partial class ConnectionsDb
    {
        /// <summary>
        /// A runtime tag to distinguish between multiple Databases
        /// </summary>
        internal readonly uint DatabaseId;


        // this is a connections database, it needs to support:
        // -> adding/removing connections by their global id.
        // -> an always sorted version by departure time.

        // a connection can be queried by:
        // - a stable global id stored in a dictionary, this is a string.
        // - an id for internal usage
        // - by enumerating them sorted by either:
        //  -> departure time

        // a connection doesn't have:
        // - delay information, just add this to the departure time. the delay offset information is just 
        //   meta data and we can store it as such. Note that a Linked Data server already gives a departure/arrival time with delay included

        // this stores the connections data:
        // - stop1 (8bytes): the departure stop id.
        // - stop2 (8bytes): the arrival stop.
        // - departure time (4bytes): seconds since 1970-1-1: 4bytes.
        // - travel time in seconds (2bytes): the travel time in seconds, max 65535 (~18H). Should be fine until we incorporate the Orient Express
        private uint
            _nextInternalId; // the next empty position in the connection data array, divided by the connection size in bytes.

        internal readonly ArrayBase<byte> _data; // the connection data.

        // this stores the connections globalId (hash of Uri) index.
        private const int _globalIdHashSize = ushort.MaxValue;
        private readonly ArrayBase<uint> _globalIdPointersPerHash;

        // ReSharper disable once RedundantDefaultMemberInitializer
        internal uint _globalIdLinkedListPointer = 0;
        internal readonly ArrayBase<uint> _globalIdLinkedList;


        // the connections meta-data, its global, trip.
        internal readonly ArrayBase<string> _globalIds; // holds the global ids.
        internal readonly ArrayBase<uint> _tripIds; // holds the trip ids.


        /// <summary>
        /// Data structure that contains multiple 'windows'.
        ///
        /// A window contains internal ids, e.g.:
        /// A window with three elements for time HH:MM
        /// [ ..., 5, 9, 12, ...]
        /// means that 5, 9 and 12 all are within HH:MM, and that the departure time of Trip 5 is smaller then the one of trip 9
        ///
        /// The metadata on the window of HH:MM is kept in _departureWindowPointers
        /// 
        /// </summary>
        internal readonly ArrayBase<uint> _departurePointers;


        /// <summary>
        /// Data structure that tracks the metadata of the departure windows
        ///
        /// Based on the departure time, the window index number is determined.
        /// (E.g: based on the time of day HH:MM, the 'hash' is 5.
        /// Then, the start of the window for this time of day can be found at _departureWindowPointers[5 * 2]
        /// The length of this window can be found at _departureWindowPointers[5 * 2 + 1]
        /// </summary>
        internal readonly ArrayBase<uint> _departureWindowPointers;


        private uint _nextDeparturePointer;

        private const uint _noData = uint.MaxValue;

        /// <summary>
        /// There is an index of connections sorted by departure time
        /// This determines the size of those windows - by default one minute -
        /// </summary>
        internal readonly uint _windowSizeInSeconds;

        /// <summary>
        /// Indicates how many windows are indexed
        /// </summary>
        internal readonly uint _numberOfWindows;

        private const int _connectionSizeInBytes = 8 + 8 + 4 + 2 + 2 + 2 + 2;


        /// <summary>
        /// The unix-time of the earliest departure time seen
        /// </summary>
        public ulong EarliestDate = ulong.MaxValue;

        /// <summary>
        /// The unix-time of the latest departure time seen
        /// </summary>
        public ulong LatestDate = ulong.MinValue;


        /// <summary>
        /// Creates a new connections db.
        ///The ConnectionsDb indexes the connections by departure time in 'departure time windows'
        ///
        /// windowSizeInSeconds indicates how big these windows are, where numberOfWindow indicates how many are provided
        ///
        /// By multiplying both, the indexable time window is provided
        /// 
        /// </summary>
        internal ConnectionsDb(uint databaseId, uint windowSizeInSeconds = 60, uint numberOfWindows = 24 * 60)
        {
            DatabaseId = databaseId;
            _windowSizeInSeconds = windowSizeInSeconds;
            _numberOfWindows = numberOfWindows;

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
                _globalIdPointersPerHash[h] = _noData;
            }

            _globalIdLinkedList = new MemoryArray<uint>(0);

            // initialize the sorting data structure for the departure pointers
            // Only a fixed amount of DepartureWindows is kept, this can be configured though
            // Note: we keep two uints for each entry:
            // one pointing into the departurePointers themself, one indicating the next index in the departureWindowPointers
            _departureWindowPointers = new MemoryArray<uint>(numberOfWindows * 2);
            for (var w = 0; w < _departureWindowPointers.Length / 2; w++)
            {
                _departureWindowPointers[w * 2 + 0] = _noData; // point to nothing.
                _departureWindowPointers[w * 2 + 1] = 0; // empty.
            }

            _departurePointers = new MemoryArray<uint>(0);
        }

        private ConnectionsDb(uint databaseId, uint windowSizeInSeconds, uint numberOfWindows, ArrayBase<byte> data,
            uint nextInternalId,
            ArrayBase<string> globalIds,
            ArrayBase<uint> tripIds, ArrayBase<uint> globalIdPointersPerHash, ArrayBase<uint> globalIdLinkedList,
            uint globalIdLinkedListPointer,
            ArrayBase<uint> departureWindowPointers, ArrayBase<uint> departurePointers, uint departurePointer,
            ulong earliestDate, ulong latestDate)
        {
            _windowSizeInSeconds = windowSizeInSeconds;
            _numberOfWindows = numberOfWindows;
            _data = data;
            _nextInternalId = nextInternalId;
            _globalIds = globalIds;
            _tripIds = tripIds;
            _globalIdLinkedListPointer = globalIdLinkedListPointer;
            _globalIdPointersPerHash = globalIdPointersPerHash;
            _globalIdLinkedList = globalIdLinkedList;

            _departureWindowPointers = departureWindowPointers;
            _departurePointers = departurePointers;
            _nextDeparturePointer = departurePointer;

            EarliestDate = earliestDate;
            LatestDate = latestDate;
            DatabaseId = databaseId;

            DatabaseIds = new[] {DatabaseId};
        }


        /// <summary>
        /// Adds a new connection.
        /// </summary>
        /// <param name="stop1">The first stop.</param>
        /// <param name="stop2">The last stop.</param>
        /// <param name="globalId">The global id.</param>
        /// <param name="departureTime">The departure time.</param>
        /// <param name="travelTime">The travel time in seconds.</param>
        /// <param name="departureDelay">The departure delay time in seconds.</param>
        /// <param name="arrivalDelay">The arrival delay time in seconds.</param>
        /// <param name="tripId">The trip id.</param>
        /// <param name="mode">The mode, indicates if getting on or off is supported</param>
        /// <returns>An internal id representing the connection in this transit db.</returns>
        internal uint AddOrUpdate(Connection newConnection)
        {
            var reader = GetReader();
            var c = new Connection();
            if (!reader.Get(newConnection.GlobalId, c))
            {
                // The connection is not yet added
                // We add the connection fresh
                return Add(newConnection);
            }

            var internalId = c.Id.InternalId;

            if (c.TripId.InternalId != newConnection.TripId.InternalId)
            {
                // trip has changed, update it.
                SetTrip(internalId, newConnection.TripId.InternalId);
            }

            var departureSeconds = (uint) newConnection.DepartureTime;
            var arrivalSeconds = (uint) (newConnection.DepartureTime + newConnection.TravelTime);


            if ((uint) c.DepartureTime == departureSeconds && (uint) c.ArrivalTime == arrivalSeconds &&
                c.DepartureDelay == newConnection.DepartureDelay &&
                c.ArrivalDelay == newConnection.ArrivalDelay &&
                c.DepartureStop.Equals(
                    newConnection.DepartureStop) &&
                c.ArrivalStop.Equals(newConnection.ArrivalStop))
            {
                // The important variables have stayed the same - no update needed
                return internalId;
            }


            // something changed - probably departure time due to delays. #SNCB
            // update the connection data.
            SetConnection(internalId, newConnection);

            if ((uint) c.DepartureTime != departureSeconds)
            {
                // update departure index if needed.
                var currentWindow = WindowFor(c.DepartureTime);
                var window = WindowFor(departureSeconds);

                if (currentWindow != window)
                {
                    // remove from current window.
                    RemoveDepartureIndex(internalId, currentWindow);

                    // add add again to new window.
                    AddDepartureIndex(internalId);
                }
                else
                {
                    // just resort the window.
                    SortDepartureWindow(window);
                }
            }

            return internalId;
        }

        /// <summary>
        /// Adds a new connection.
        /// </summary>
        /// <param name="stop1">The first stop.</param>
        /// <param name="stop2">The last stop.</param>
        /// <param name="globalId">The global id.</param>
        /// <param name="departureTime">The departure time.</param>
        /// <param name="travelTime">The travel time in seconds.</param>
        /// <param name="departureDelay">The departure delay time in seconds.</param>
        /// <param name="arrivalDelay">The arrival delay time in seconds.</param>
        /// <param name="tripId">The trip id.</param>
        /// <param name="mode">The trip mode</param>
        /// <returns>An internal id representing the connection in this transit db.</returns>
        private uint Add(Connection c)
        {
            // get the next internal id.
            var internalId = _nextInternalId;
            _nextInternalId++;

            // set this connection info int the data array.
            var departureSeconds = (uint) c.DepartureTime;
            SetConnection(internalId, c);

            // check if this connections is the 'earliest' or 'latest' date-wise.
            var departureDateSeconds = departureSeconds;
            if (departureDateSeconds < EarliestDate)
            {
                EarliestDate = departureDateSeconds;
            }

            if (departureDateSeconds > LatestDate)
            {
                LatestDate = departureDateSeconds;
            }

            // set trip and global ids.
            SetTrip(internalId, c.TripId.InternalId);
            SetGlobalId(internalId, c.GlobalId);

            // update departure time index.
            AddDepartureIndex(internalId);

            return internalId;
        }

        private void SetConnection(uint internalId, Connection c)
        {
            // make sure the data array is big enough.
            var dataPointer = internalId * _connectionSizeInBytes;
            while (_data.Length <= dataPointer + _connectionSizeInBytes)
            {
                var oldLength = _data.Length;
                _data.Resize(_data.Length + 1024);
                for (var i = oldLength; i < _data.Length; i++)
                {
                    _data[i] = byte.MaxValue;
                }
            }


            // Start saving the data
            var offset = 0;


            // Note that the database id of the location is _not_ saved
            var bytes = BitConverter.GetBytes(c.DepartureStop.LocalTileId);
            for (var b = 0; b < 4; b++)
            {
                _data[dataPointer + offset + b] = bytes[b];
            }

            offset += 4;

            bytes = BitConverter.GetBytes(c.DepartureStop.LocalId);
            for (var b = 0; b < 4; b++)
            {
                _data[dataPointer + offset + b] = bytes[b];
            }

            offset += 4;

            bytes = BitConverter.GetBytes(c.ArrivalStop.LocalTileId);
            for (var b = 0; b < 4; b++)
            {
                _data[dataPointer + offset + b] = bytes[b];
            }

            offset += 4;
            bytes = BitConverter.GetBytes(c.ArrivalStop.LocalId);
            for (var b = 0; b < 4; b++)
            {
                _data[dataPointer + offset + b] = bytes[b];
            }

            offset += 4;
            bytes = BitConverter.GetBytes(c.DepartureTime);
            for (var b = 0; b < 4; b++)
            {
                _data[dataPointer + offset + b] = bytes[b];
            }

            offset += 4;
            bytes = BitConverter.GetBytes(c.TravelTime);
            for (var b = 0; b < 2; b++)
            {
                _data[dataPointer + offset + b] = bytes[b];
            }

            offset += 2;
            bytes = BitConverter.GetBytes(c.DepartureDelay);
            for (var b = 0; b < 2; b++)
            {
                _data[dataPointer + offset + b] = bytes[b];
            }

            offset += 2;
            bytes = BitConverter.GetBytes(c.ArrivalDelay);
            for (var b = 0; b < 2; b++)
            {
                _data[dataPointer + offset + b] = bytes[b];
            }

            offset += 2;
            bytes = BitConverter.GetBytes(c.Mode);
            for (var b = 0; b < 2; b++)
            {
                _data[dataPointer + offset + b] = bytes[b];
            }

            offset += 2;

            if (offset != _connectionSizeInBytes)
            {
                throw new ArgumentException($"Only wrote {offset} bytes while {_connectionSizeInBytes} expected");
            }
        }

        [Pure]
        private bool
            GetConnection(ConnectionId id, Connection writeTo)
        {
            if (id.DatabaseId != DatabaseId)
            {
                return false;
            }

            writeTo.Id = id;
            var dataPointer = id.InternalId * _connectionSizeInBytes;
            if (_data.Length <= dataPointer + _connectionSizeInBytes)
            {
                return false;
            }

            var bytes = new byte[_connectionSizeInBytes];
            for (var b = 0; b < _connectionSizeInBytes; b++)
            {
                bytes[b] = _data[dataPointer + b];
            }

            var offset = 0;

            var departureStop =
                new StopId(DatabaseId,
                    BitConverter.ToUInt32(bytes, 0),
                    BitConverter.ToUInt32(bytes, 4));
            if (departureStop.LocalTileId == uint.MaxValue &&
                departureStop.LocalId == uint.MaxValue)
            {
                return false;
            }

            writeTo.DepartureStop = departureStop;
            offset += 8;

            writeTo.ArrivalStop = new StopId(DatabaseId,
                BitConverter.ToUInt32(bytes, offset + 0),
                BitConverter.ToUInt32(bytes, offset + 4));

            offset += 8;
            writeTo.DepartureTime = BitConverter.ToUInt32(bytes, offset);
            offset += 4;
            writeTo.TravelTime = BitConverter.ToUInt16(bytes, offset);
            offset += 2;
            writeTo.DepartureDelay = BitConverter.ToUInt16(bytes, offset);
            offset += 2;
            writeTo.ArrivalDelay = BitConverter.ToUInt16(bytes, offset);
            offset += 2;
            writeTo.Mode = BitConverter.ToUInt16(bytes, offset);
            offset += 2;
            writeTo.GlobalId = GetGlobalId(id.InternalId);
            writeTo.TripId = new TripId(DatabaseId, _tripIds[id.InternalId]);

            writeTo.ArrivalTime = writeTo.DepartureTime + writeTo.TravelTime;

            return true;
        }

        [Pure]
        private uint GetConnectionDeparture(uint internalId)
        {
            var dataPointer = internalId * _connectionSizeInBytes;
            if (_data.Length <= dataPointer + _connectionSizeInBytes)
            {
                return uint.MaxValue;
            }

            var bytes = new byte[4];
            for (var b = 0; b < 4; b++)
            {
                bytes[b] = _data[dataPointer + 16 + b];
            }

            return BitConverter.ToUInt32(bytes, 0);
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
            var linkedListPointer = _globalIdLinkedListPointer;
            _globalIdLinkedListPointer += 2;
            while (_globalIdLinkedList.Length <= linkedListPointer)
            {
                _globalIdLinkedList.Resize(_globalIdLinkedList.Length + 1024);
            }

            var hash = Hash(globalId);

            _globalIdLinkedList[linkedListPointer + 0] = internalId;
            _globalIdLinkedList[linkedListPointer + 1] = _globalIdPointersPerHash[hash];
            _globalIdPointersPerHash[hash] = linkedListPointer + 0;
        }

        private string GetGlobalId(uint internalId)
        {
            return _globalIds[internalId];
        }

        [Pure]
        private static uint Hash(string id)
        {
            // https://stackoverflow.com/questions/5154970/how-do-i-create-a-hashcode-in-net-c-for-a-string-that-is-safe-to-store-in-a
            unchecked
            {
                uint hash = 23;
                foreach (var c in id)
                {
                    hash = hash * 31 + c;
                }

                return hash % _globalIdHashSize;
            }
        }

        /// <summary>
        /// Generate a window-number for this dateTime
        /// This works as an initial hash of the dateTime
        /// </summary>
        /// <param name="unixTime"></param>
        /// <returns></returns>
        [Pure]
        internal uint WindowFor(ulong unixTime)
        {
            return (uint) ((unixTime / _windowSizeInSeconds) % _numberOfWindows);
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private bool RemoveDepartureIndex(uint internalId, uint window)
        {
            var windowPointer = _departureWindowPointers[window * 2 + 0];
            if (_departureWindowPointers[window * 2 + 0] == _noData)
            {
                // nothing to remove.
                return false;
            }

            var windowSize = _departureWindowPointers[window * 2 + 1];

            // find entry.
            for (var p = windowPointer; p < windowPointer + windowSize; p++)
            {
                var id = _departurePointers[p];
                if (id != internalId) continue;

                // move all after one down.
                for (; p < windowPointer + windowSize - 1; p++)
                {
                    _departurePointers[p] = _departurePointers[p + 1];
                }

                // decrease window size.
                _departureWindowPointers[window * 2 + 1] = windowSize - 1;
                return true;
            }

            return false;
        }


        /// <summary>
        /// Checks that the window at the given index and the given size has room to grow.
        /// If not, it is copied over to fresh memory.
        /// The pointer to use is returned (either the old or new one)
        /// </summary>
        /// <param name="windowPointer"></param>
        /// <param name="windowSize"></param>
        private uint IncreaseWindowSizeIfNeeded(uint windowPointer, uint windowSize)
        {
            if ((windowSize & (windowSize - 1)) != 0)
            {
                return windowPointer;
            }


            // The current window size is a power of 2
            // Time to double the window capacity by allocating new space.
            var newWindowPointer = _nextDeparturePointer;

            // We'll need windowSize * 2 more room, so a new window pointer should point to here
            _nextDeparturePointer += windowSize * 2;

            // Increase the size of the array as long as is needed, in chunks of 1024 bytes
            while (_departurePointers.Length <= _nextDeparturePointer)
            {
                _departurePointers.Resize(_departurePointers.Length + 1024);
            }

            // copy over data.
            for (var c = 0; c < windowSize; c++)
            {
                _departurePointers[newWindowPointer + c] =
                    _departurePointers[windowPointer + c];
            }

            return newWindowPointer;
        }

        private void AddDepartureIndex(uint internalId)
        {
            // The departure time of the connection - nothing fancy here.
            var departure = GetConnectionDeparture(internalId);

            // The window number for this connection, based on the time of day
            // The first minute will have 'window = 0', the second minute will have 1, '01:01 AM' will have number 61
            var window = WindowFor(departure);

            var nextEmpty = uint.MaxValue;

            // Where, in _departurePointers can we find this window?
            // This is kept by _departureWindowPointers
            var windowPointer = _departureWindowPointers[window * 2 + 0];
            if (windowPointer == _noData)
            {
                // add a new window.
                nextEmpty = _nextDeparturePointer;
                _nextDeparturePointer += 1;

                // update the window.
                _departureWindowPointers[window * 2 + 0] = nextEmpty;
                _departureWindowPointers[window * 2 + 1] = 1;
            }
            else
            {
                // there is already data in the window.
                var windowSize = _departureWindowPointers[window * 2 + 1];

                // If needed, increase the size
                windowPointer = IncreaseWindowSizeIfNeeded(windowPointer, windowSize);
                // Save the new windowpointer. Won't do anything in 90% of the cases
                _departureWindowPointers[window * 2 + 0] = windowPointer;
                // Increase the windowsize
                _departureWindowPointers[window * 2 + 1] = windowSize + 1;
                // Increase the nextEmpty pointer
                nextEmpty = windowPointer + windowSize;
            }

            // If needed, we allocate another chunk of memory
            while (_departurePointers.Length <= nextEmpty)
            {
                _departurePointers.Resize(_departurePointers.Length + 1024);
            }

            // set the data.
            _departurePointers[nextEmpty] = internalId;

            // sort the window.
            SortDepartureWindow(window);
        }

        private void SortDepartureWindow(uint window)
        {
            var windowPointer = _departureWindowPointers[window * 2 + 0];
            var windowSize = _departureWindowPointers[window * 2 + 1];
            QuickSort.Sort(i => GetConnectionDeparture(_departurePointers[i]),
                (i1, i2) =>
                {
                    var temp = _departurePointers[i1];
                    _departurePointers[i1] = _departurePointers[i2];
                    _departurePointers[i2] = temp;
                }, windowPointer, windowPointer + windowSize - 1);
        }


        /// <summary>
        /// Returns a deep in-memory copy.
        /// </summary>
        /// <returns></returns>
        [Pure]
        public ConnectionsDb Clone()
        {
            var data = new MemoryArray<byte>(_data.Length);
            data.CopyFrom(_data, _data.Length);
            var globalIds = new MemoryArray<string>(_globalIds.Length);
            globalIds.CopyFrom(_globalIds, _globalIds.Length);
            var tripIds = new MemoryArray<uint>(_tripIds.Length);
            tripIds.CopyFrom(_tripIds, _tripIds.Length);
            var globalIdPointersPerHash = new MemoryArray<uint>(_globalIdPointersPerHash.Length);
            globalIdPointersPerHash.CopyFrom(_globalIdPointersPerHash, _globalIdPointersPerHash.Length);
            var globalIdLinkedList = new MemoryArray<uint>(_globalIdLinkedList.Length);
            globalIdLinkedList.CopyFrom(_globalIdLinkedList, _globalIdLinkedList.Length);
            var departureWindowPointers = new MemoryArray<uint>(_departureWindowPointers.Length);
            departureWindowPointers.CopyFrom(_departureWindowPointers, _departureWindowPointers.Length);
            var departurePointers = new MemoryArray<uint>(_departurePointers.Length);
            departurePointers.CopyFrom(_departurePointers, _departurePointers.Length);
            return new ConnectionsDb(
                DatabaseId,
                _windowSizeInSeconds, _numberOfWindows, data, _nextInternalId, globalIds, tripIds,
                globalIdPointersPerHash, globalIdLinkedList,
                _globalIdLinkedListPointer, departureWindowPointers, departurePointers, _nextDeparturePointer,
                EarliestDate, LatestDate);
        }

        internal long WriteTo(Stream stream)
        {
            // Count the number of bytes, which will be returned
            var length = 0L;

            // write version #.
            stream.WriteByte(2);
            length++;

            // Write the five big data structures
            length += _data.CopyToWithSize(stream);
            length += _globalIds.CopyToWithSize(stream);
            length += _tripIds.CopyToWithSize(stream);
            length += _globalIdPointersPerHash.CopyToWithSize(stream);
            length += _globalIdLinkedList.CopyToWithSize(stream);

            // Write the linkedListPointer for global ids
            var bytes = BitConverter.GetBytes(_globalIdLinkedListPointer);
            stream.Write(bytes, 0, 4);
            length += 4;

            // Write the departure window data + size
            length += _departureWindowPointers.CopyToWithSize(stream);
            length += _departurePointers.CopyToWithSize(stream);
            bytes = BitConverter.GetBytes(_nextDeparturePointer);
            stream.Write(bytes, 0, 4);
            length += 4;

            // Three other ints: windowSize, numbers and interalIdPOinter
            bytes = BitConverter.GetBytes(_windowSizeInSeconds);
            stream.Write(bytes, 0, 4);
            length += 4;
            bytes = BitConverter.GetBytes(_numberOfWindows);
            stream.Write(bytes, 0, 4);
            length += 4;
            bytes = BitConverter.GetBytes(_nextInternalId);
            stream.Write(bytes, 0, 4);
            length += 4;

            // And two longs: start-and-enddate
            bytes = BitConverter.GetBytes(EarliestDate);
            stream.Write(bytes, 0, 8);
            length += 4;
            bytes = BitConverter.GetBytes(LatestDate);
            stream.Write(bytes, 0, 8);
            length += 4;
            return length;
        }

        [Pure]
        internal static ConnectionsDb ReadFrom(Stream stream, uint databaseId)
        {
            var buffer = new byte[8];

            // Read and validate the version number
            var version = stream.ReadByte();
            if (version != 2)
                throw new InvalidDataException($"Cannot read {nameof(ConnectionsDb)}, invalid version #.");

            // Read the five big data structures
            var data = MemoryArray<byte>.CopyFromWithSize(stream);
            var globalIds = MemoryArray<string>.CopyFromWithSize(stream);
            var tripIds = MemoryArray<uint>.CopyFromWithSize(stream);
            var globalIdPointersPerHash = MemoryArray<uint>.CopyFromWithSize(stream);
            var globalIdLinkedList = MemoryArray<uint>.CopyFromWithSize(stream);

            // Read the linkedListPointer for global ids
            stream.Read(buffer, 0, 4);
            var globalIdLinkedListPointer = BitConverter.ToUInt32(buffer, 0);

            // Read the departure window data + size
            var departureWindowPointers = MemoryArray<uint>.CopyFromWithSize(stream);
            var departurePointers = MemoryArray<uint>.CopyFromWithSize(stream);
            stream.Read(buffer, 0, 4);
            var departurePointer = BitConverter.ToUInt32(buffer, 0);

            // Three other ints: windowSize, numbers and interalIdPOinter
            stream.Read(buffer, 0, 4);
            var windowSizeInSeconds = BitConverter.ToUInt32(buffer, 0);
            stream.Read(buffer, 0, 4);
            var numberOfWindows = BitConverter.ToUInt32(buffer, 0);
            stream.Read(buffer, 0, 4);
            var nextInternalId = BitConverter.ToUInt32(buffer, 0);

            // Two longs: start- and enddate
            stream.Read(buffer, 0, 8);
            var earliestDate = BitConverter.ToUInt64(buffer, 0);
            stream.Read(buffer, 0, 8);
            var latestDate = BitConverter.ToUInt64(buffer, 0);


            // Lets wrap it all up!
            return new ConnectionsDb(
                databaseId, // Database ID's are not serialized
                windowSizeInSeconds, numberOfWindows, data, nextInternalId, globalIds, tripIds,
                globalIdPointersPerHash, globalIdLinkedList, globalIdLinkedListPointer,
                departureWindowPointers, departurePointers, departurePointer,
                earliestDate, latestDate);
        }


        /// <summary>
        /// Gets a reader.
        /// </summary>
        /// <returns></returns>
        [Pure]
        public ConnectionsDb GetReader()
        {
            // TODO REMOVE THIS
            return this;
        }

        public IEnumerable<uint> DatabaseIds { get; }


        /// <summary>
        /// Gets an enumerator enumerating connections sorted by their departure time.
        /// </summary>
        /// <returns>The departure enumerator.</returns>
        [Pure]
        public DepartureEnumerator GetDepartureEnumerator()
        {
            return new DepartureEnumerator(this);
        }
    }
}