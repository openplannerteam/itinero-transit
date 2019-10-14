using System;
using System.Diagnostics.Contracts;
using System.IO;
using Itinero.Transit.Data.Core;
using Reminiscence;
using Reminiscence.Arrays;

namespace Itinero.Transit.Data
{
    public partial class ConnectionsDb
    {
        /// <summary>
        /// Returns a deep in-memory copy.
        /// </summary>
        /// <returns></returns>
        [Pure]
        public IDatabase<ConnectionId, Connection> Clone()
        {
            var data = new MemoryArray<byte>(Data.Length);
            data.CopyFrom(Data, Data.Length);
            var globalIds = new MemoryArray<string>(GlobalIds.Length);
            globalIds.CopyFrom(GlobalIds, GlobalIds.Length);
            var tripIds = new MemoryArray<uint>(_tripIds.Length);
            tripIds.CopyFrom(_tripIds, _tripIds.Length);
            var globalIdPointersPerHash = new MemoryArray<uint>(_globalIdPointersPerHash.Length);
            globalIdPointersPerHash.CopyFrom(_globalIdPointersPerHash, _globalIdPointersPerHash.Length);
            var globalIdLinkedList = new MemoryArray<uint>(GlobalIdLinkedList.Length);
            globalIdLinkedList.CopyFrom(GlobalIdLinkedList, GlobalIdLinkedList.Length);
            var departureWindowPointers = new MemoryArray<uint>(DepartureWindowPointers.Length);
            departureWindowPointers.CopyFrom(DepartureWindowPointers, DepartureWindowPointers.Length);
            var departurePointers = new MemoryArray<uint>(DeparturePointers.Length);
            departurePointers.CopyFrom(DeparturePointers, DeparturePointers.Length);
            return new ConnectionsDb(
                DatabaseId,
                WindowSizeInSeconds, NumberOfWindows, data, _nextInternalId, globalIds, tripIds,
                globalIdPointersPerHash, globalIdLinkedList,
                GlobalIdLinkedListPointer, departureWindowPointers, departurePointers, _nextDeparturePointer,
                EarliestDate, LatestDate);
        }

        public long WriteTo(Stream stream)
        {
            // Count the number of bytes, which will be returned
            var length = 0L;

            // write version #.
            stream.WriteByte(2);
            length++;

            // Write the five big data structures
            length += Data.CopyToWithSize(stream);
            length += GlobalIds.CopyToWithSize(stream);
            length += _tripIds.CopyToWithSize(stream);
            length += _globalIdPointersPerHash.CopyToWithSize(stream);
            length += GlobalIdLinkedList.CopyToWithSize(stream);

            // Write the linkedListPointer for global ids
            var bytes = BitConverter.GetBytes(GlobalIdLinkedListPointer);
            stream.Write(bytes, 0, 4);
            length += 4;

            // Write the departure window data + size
            length += DepartureWindowPointers.CopyToWithSize(stream);
            length += DeparturePointers.CopyToWithSize(stream);
            bytes = BitConverter.GetBytes(_nextDeparturePointer);
            stream.Write(bytes, 0, 4);
            length += 4;

            // Three other ints: windowSize, numbers and interalIdPOinter
            bytes = BitConverter.GetBytes(WindowSizeInSeconds);
            stream.Write(bytes, 0, 4);
            length += 4;
            bytes = BitConverter.GetBytes(NumberOfWindows);
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
    }
}