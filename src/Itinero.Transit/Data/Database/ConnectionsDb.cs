using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Itinero.Transit.Algorithms.Sorting;
using Itinero.Transit.Data.Core;
using Reminiscence.Arrays;

// ReSharper disable RedundantAssignment

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Functional")]

namespace Itinero.Transit.Data.ReminiscenceConnectionsDb
{
    public partial class ConnectionsDb : IConnectionsDb
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

        internal readonly ArrayBase<byte> Data; // the connection data.

        // this stores the connections globalId (hash of Uri) index.
        private const int _globalIdHashSize = ushort.MaxValue;
        private readonly ArrayBase<uint> _globalIdPointersPerHash;

        // ReSharper disable once RedundantDefaultMemberInitializer
        internal uint GlobalIdLinkedListPointer = 0;
        internal readonly ArrayBase<uint> GlobalIdLinkedList;


        // the connections meta-data, its global, trip.
        internal readonly ArrayBase<string> GlobalIds; // holds the global ids.
        private readonly ArrayBase<uint> _tripIds; // holds the trip ids.


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
        internal readonly ArrayBase<uint> DeparturePointers;


        /// <summary>
        /// Data structure that tracks the metadata of the departure windows
        ///
        /// Based on the departure time, the window index number is determined.
        /// (E.g: based on the time of day HH:MM, the 'hash' is 5.
        /// Then, the start of the window for this time of day can be found at _departureWindowPointers[5 * 2]
        /// The length of this window can be found at _departureWindowPointers[5 * 2 + 1]
        /// </summary>
        internal readonly ArrayBase<uint> DepartureWindowPointers;


        private uint _nextDeparturePointer;

        private const uint _noData = uint.MaxValue;

        /// <summary>
        /// There is an index of connections sorted by departure time
        /// This determines the size of those windows - by default one minute -
        /// </summary>
        internal readonly uint WindowSizeInSeconds;

        /// <summary>
        /// Indicates how many windows are indexed
        /// </summary>
        internal readonly uint NumberOfWindows;

        private const int _connectionSizeInBytes = 8 + 8 + 4 + 2 + 2 + 2 + 2;


        /// <summary>
        /// The unix-time of the earliest departure time seen
        /// </summary>
        public ulong EarliestDate { get; private set; } = ulong.MaxValue;

        /// <summary>
        /// The unix-time of the latest departure time seen
        /// </summary>
        public ulong LatestDate { get; private set; } = ulong.MinValue;


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
            WindowSizeInSeconds = windowSizeInSeconds;
            NumberOfWindows = numberOfWindows;

            // initialize the data array.
            Data = new MemoryArray<byte>(0);
            _nextInternalId = 0;

            // initialize the meta-data arrays.
            GlobalIds = new MemoryArray<string>(0);
            _tripIds = new MemoryArray<uint>(0);
            _nextInternalId = 0;

            // initialize the ids reverse index.
            _globalIdPointersPerHash = new MemoryArray<uint>(_globalIdHashSize);
            for (var h = 0; h < _globalIdPointersPerHash.Length; h++)
            {
                _globalIdPointersPerHash[h] = _noData;
            }

            GlobalIdLinkedList = new MemoryArray<uint>(0);

            // initialize the sorting data structure for the departure pointers
            // Only a fixed amount of DepartureWindows is kept, this can be configured though
            // Note: we keep two uints for each entry:
            // one pointing into the departurePointers themself, one indicating the next index in the departureWindowPointers
            DepartureWindowPointers = new MemoryArray<uint>(numberOfWindows * 2);
            for (var w = 0; w < DepartureWindowPointers.Length / 2; w++)
            {
                DepartureWindowPointers[w * 2 + 0] = _noData; // point to nothing.
                DepartureWindowPointers[w * 2 + 1] = 0; // empty.
            }

            DeparturePointers = new MemoryArray<uint>(0);
        }

        private ConnectionsDb(uint databaseId, uint windowSizeInSeconds, uint numberOfWindows, ArrayBase<byte> data,
            uint nextInternalId,
            ArrayBase<string> globalIds,
            ArrayBase<uint> tripIds, ArrayBase<uint> globalIdPointersPerHash, ArrayBase<uint> globalIdLinkedList,
            uint globalIdLinkedListPointer,
            ArrayBase<uint> departureWindowPointers, ArrayBase<uint> departurePointers, uint departurePointer,
            ulong earliestDate, ulong latestDate)
        {
            WindowSizeInSeconds = windowSizeInSeconds;
            NumberOfWindows = numberOfWindows;
            Data = data;
            _nextInternalId = nextInternalId;
            GlobalIds = globalIds;
            _tripIds = tripIds;
            GlobalIdLinkedListPointer = globalIdLinkedListPointer;
            _globalIdPointersPerHash = globalIdPointersPerHash;
            GlobalIdLinkedList = globalIdLinkedList;

            DepartureWindowPointers = departureWindowPointers;
            DeparturePointers = departurePointers;
            _nextDeparturePointer = departurePointer;

            EarliestDate = earliestDate;
            LatestDate = latestDate;
            DatabaseId = databaseId;

            DatabaseIds = new[] {DatabaseId};
        }


        public ConnectionId AddOrUpdate(Connection newConnection)
        {
            var c = new Connection();
            if (!Get(newConnection.GlobalId, c))
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
                return c.Id;
            }


            // something changed - probably departure time due to delays. #SNCB
            // update the connection data.
            SetConnection(internalId, newConnection);

            if ((uint) c.DepartureTime == departureSeconds) return c.Id;
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

            return c.Id;
        }

        public ConnectionId Add(Connection c)
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

            return new ConnectionId(DatabaseId, internalId);
        }

        private void SetConnection(uint internalId, Connection c)
        {
            // make sure the data array is big enough.
            var dataPointer = internalId * _connectionSizeInBytes;
            while (Data.Length <= dataPointer + _connectionSizeInBytes)
            {
                var oldLength = Data.Length;
                Data.Resize(Data.Length + 1024);
                for (var i = oldLength; i < Data.Length; i++)
                {
                    Data[i] = byte.MaxValue;
                }
            }


            // Start saving the data
            var offset = 0;


            // Note that the database id of the location is _not_ saved
            var bytes = BitConverter.GetBytes(c.DepartureStop.LocalTileId);
            for (var b = 0; b < 4; b++)
            {
                Data[dataPointer + offset + b] = bytes[b];
            }

            offset += 4;

            bytes = BitConverter.GetBytes(c.DepartureStop.LocalId);
            for (var b = 0; b < 4; b++)
            {
                Data[dataPointer + offset + b] = bytes[b];
            }

            offset += 4;

            bytes = BitConverter.GetBytes(c.ArrivalStop.LocalTileId);
            for (var b = 0; b < 4; b++)
            {
                Data[dataPointer + offset + b] = bytes[b];
            }

            offset += 4;
            bytes = BitConverter.GetBytes(c.ArrivalStop.LocalId);
            for (var b = 0; b < 4; b++)
            {
                Data[dataPointer + offset + b] = bytes[b];
            }

            offset += 4;
            bytes = BitConverter.GetBytes(c.DepartureTime);
            for (var b = 0; b < 4; b++)
            {
                Data[dataPointer + offset + b] = bytes[b];
            }

            offset += 4;
            bytes = BitConverter.GetBytes(c.TravelTime);
            for (var b = 0; b < 2; b++)
            {
                Data[dataPointer + offset + b] = bytes[b];
            }

            offset += 2;
            bytes = BitConverter.GetBytes(c.DepartureDelay);
            for (var b = 0; b < 2; b++)
            {
                Data[dataPointer + offset + b] = bytes[b];
            }

            offset += 2;
            bytes = BitConverter.GetBytes(c.ArrivalDelay);
            for (var b = 0; b < 2; b++)
            {
                Data[dataPointer + offset + b] = bytes[b];
            }

            offset += 2;
            bytes = BitConverter.GetBytes(c.Mode);
            for (var b = 0; b < 2; b++)
            {
                Data[dataPointer + offset + b] = bytes[b];
            }

            offset += 2;

            if (offset != _connectionSizeInBytes)
            {
                throw new ArgumentException($"Only wrote {offset} bytes while {_connectionSizeInBytes} expected");
            }
        }

        [Pure]
        public bool Get(ConnectionId id, Connection writeTo)
        {
            if (id.DatabaseId != DatabaseId)
            {
                return false;
            }

            writeTo.Id = id;
            var dataPointer = id.InternalId * _connectionSizeInBytes;
            if (Data.Length <= dataPointer + _connectionSizeInBytes)
            {
                return false;
            }

            var bytes = new byte[_connectionSizeInBytes];
            for (var b = 0; b < _connectionSizeInBytes; b++)
            {
                bytes[b] = Data[dataPointer + b];
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
            if (Data.Length <= dataPointer + _connectionSizeInBytes)
            {
                return uint.MaxValue;
            }

            var bytes = new byte[4];
            for (var b = 0; b < 4; b++)
            {
                bytes[b] = Data[dataPointer + 16 + b];
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
            while (GlobalIds.Length <= internalId)
            {
                GlobalIds.Resize(GlobalIds.Length + 1024);
            }

            GlobalIds[internalId] = globalId;

            // add stop id to the index.
            var linkedListPointer = GlobalIdLinkedListPointer;
            GlobalIdLinkedListPointer += 2;
            while (GlobalIdLinkedList.Length <= linkedListPointer)
            {
                GlobalIdLinkedList.Resize(GlobalIdLinkedList.Length + 1024);
            }

            var hash = Hash(globalId);

            GlobalIdLinkedList[linkedListPointer + 0] = internalId;
            GlobalIdLinkedList[linkedListPointer + 1] = _globalIdPointersPerHash[hash];
            _globalIdPointersPerHash[hash] = linkedListPointer + 0;
        }

        private string GetGlobalId(uint internalId)
        {
            return GlobalIds[internalId];
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
            return (uint) ((unixTime / WindowSizeInSeconds) % NumberOfWindows);
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private bool RemoveDepartureIndex(uint internalId, uint window)
        {
            var windowPointer = DepartureWindowPointers[window * 2 + 0];
            if (DepartureWindowPointers[window * 2 + 0] == _noData)
            {
                // nothing to remove.
                return false;
            }

            var windowSize = DepartureWindowPointers[window * 2 + 1];

            // find entry.
            for (var p = windowPointer; p < windowPointer + windowSize; p++)
            {
                var id = DeparturePointers[p];
                if (id != internalId) continue;

                // move all after one down.
                for (; p < windowPointer + windowSize - 1; p++)
                {
                    DeparturePointers[p] = DeparturePointers[p + 1];
                }

                // decrease window size.
                DepartureWindowPointers[window * 2 + 1] = windowSize - 1;
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
            while (DeparturePointers.Length <= _nextDeparturePointer)
            {
                DeparturePointers.Resize(DeparturePointers.Length + 1024);
            }

            // copy over data.
            for (var c = 0; c < windowSize; c++)
            {
                DeparturePointers[newWindowPointer + c] =
                    DeparturePointers[windowPointer + c];
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
            var windowPointer = DepartureWindowPointers[window * 2 + 0];
            if (windowPointer == _noData)
            {
                // add a new window.
                nextEmpty = _nextDeparturePointer;
                _nextDeparturePointer += 1;

                // update the window.
                DepartureWindowPointers[window * 2 + 0] = nextEmpty;
                DepartureWindowPointers[window * 2 + 1] = 1;
            }
            else
            {
                // there is already data in the window.
                var windowSize = DepartureWindowPointers[window * 2 + 1];

                // If needed, increase the size
                windowPointer = IncreaseWindowSizeIfNeeded(windowPointer, windowSize);
                // Save the new windowpointer. Won't do anything in 90% of the cases
                DepartureWindowPointers[window * 2 + 0] = windowPointer;
                // Increase the windowsize
                DepartureWindowPointers[window * 2 + 1] = windowSize + 1;
                // Increase the nextEmpty pointer
                nextEmpty = windowPointer + windowSize;
            }

            // If needed, we allocate another chunk of memory
            while (DeparturePointers.Length <= nextEmpty)
            {
                DeparturePointers.Resize(DeparturePointers.Length + 1024);
            }

            // set the data.
            DeparturePointers[nextEmpty] = internalId;

            // sort the window.
            SortDepartureWindow(window);
        }

        private void SortDepartureWindow(uint window)
        {
            var windowPointer = DepartureWindowPointers[window * 2 + 0];
            var windowSize = DepartureWindowPointers[window * 2 + 1];
            Action<long, long> swap = (i1, i2) =>
            {
                var temp = DeparturePointers[i1];
                DeparturePointers[i1] = DeparturePointers[i2];
                DeparturePointers[i2] = temp;
            };
            Sorting.Sort(i => GetConnectionDeparture(DeparturePointers[i]), swap
                , windowPointer, windowPointer + windowSize - 1);


          // TODO Add sort based on assumptions, see https://github.com/openplannerteam/itinero-transit/issues/82
        }


        public IEnumerable<uint> DatabaseIds { get; }
    }
}