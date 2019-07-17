using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.Data.Core;
using Reminiscence;
using Reminiscence.Arrays;
using Attribute = Itinero.Transit.Data.Attributes.Attribute;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]

namespace Itinero.Transit.Data
{
    /// <summary>
    /// A trips database.
    /// </summary>
    public class TripsDb : IDatabaseReader<TripId, Trip>
    {
        private readonly ArrayBase<string> _tripIds; // holds the trip ids.
        private readonly ArrayBase<uint> _tripAttributeIds; // holds the trip attribute ids.

        private readonly int _tripIdHashSize = ushort.MaxValue;
        private const uint _noData = uint.MaxValue;
        private readonly ArrayBase<uint> _tripIdPointersPerHash;
        private uint _tripIdLinkedListPointer;
        private readonly ArrayBase<uint> _tripIdLinkedList;

        private readonly AttributesIndex _attributes;
        private uint _nextId;
        private readonly uint _dbId;
        public IEnumerable<uint> DatabaseIds { get; }

        /// <summary>
        /// Creates a new trips database.
        /// </summary>
        internal TripsDb(uint dbId)
        {
            _dbId = dbId;
            _tripIds = new MemoryArray<string>(0);
            _tripAttributeIds = new MemoryArray<uint>(0);
            _tripIdPointersPerHash = new MemoryArray<uint>(_tripIdHashSize);
            for (var h = 0; h < _tripIdPointersPerHash.Length; h++)
            {
                _tripIdPointersPerHash[h] = _noData;
            }

            _tripIdLinkedList = new MemoryArray<uint>(0);
            _attributes = new AttributesIndex();
            DatabaseIds = new[] {dbId};
        }

        private TripsDb(
            uint dbId,
            ArrayBase<string> tripIds, ArrayBase<uint> tripAttributeIds, ArrayBase<uint> tripIdPointersPerHash,
            ArrayBase<uint> tripIdLinkedList, AttributesIndex attributes, uint tripIdLinkedListPointer, uint nextId,
            int tripIdHashSize)
        {
            _tripIdHashSize = tripIdHashSize;
            _dbId = dbId;
            _tripIds = tripIds;
            _tripAttributeIds = tripAttributeIds;
            _tripIdPointersPerHash = tripIdPointersPerHash;
            _tripIdLinkedList = tripIdLinkedList;
            _attributes = attributes;
            _tripIdLinkedListPointer = tripIdLinkedListPointer;
            _nextId = nextId; 
            DatabaseIds = new[] {dbId};
        }

        /// <summary>
        /// Adds a new trip.
        /// </summary>
        /// <param name="globalId">The global id.</param>
        /// <param name="attributes">The attributes.</param>
        /// <returns>The trip id.</returns>
        internal TripId Add(string globalId, IEnumerable<Attribute> attributes = null)
        {
            var tripId = _nextId;
            _nextId++;
            while (_tripIds.Length <= tripId)
            {
                _tripIds.Resize(_tripIds.Length + 1024);
                _tripAttributeIds.Resize(_tripAttributeIds.Length + 1024);
            }

            _tripIds[tripId] = globalId;
            _tripAttributeIds[tripId] = _attributes.Add(attributes);

            // add stop id to the index.
            _tripIdLinkedListPointer += 2;
            while (_tripIdLinkedList.Length <= _tripIdLinkedListPointer)
            {
                _tripIdLinkedList.Resize(_tripIdLinkedList.Length + 1024);
            }

            var hash = Hash(globalId);
            _tripIdLinkedList[_tripIdLinkedListPointer - 2] = tripId;
            _tripIdLinkedList[_tripIdLinkedListPointer - 1] = _tripIdPointersPerHash[hash];
            _tripIdPointersPerHash[hash] = _tripIdLinkedListPointer - 2;

            return new TripId(_dbId, tripId);
        }

        private uint Hash(string id)
        {
            // https://stackoverflow.com/questions/5154970/how-do-i-create-a-hashcode-in-net-c-for-a-string-that-is-safe-to-store-in-a
            unchecked
            {
                var hash = (uint) 23;
                foreach (var c in id)
                {
                    hash = hash * 31 + c;
                }

                return (uint) (hash % _tripIdHashSize);
            }
        }

        internal long WriteTo(Stream stream)
        {
            var length = 0L;

            // write version #.
            stream.WriteByte(1);
            length++;

            // write data.
            length += _tripIds.CopyToWithSize(stream);
            length += _tripAttributeIds.CopyToWithSize(stream);
            length += _tripIdPointersPerHash.CopyToWithSize(stream);
            length += _tripIdLinkedList.CopyToWithSize(stream);

            // write pointers.
            var bytes = BitConverter.GetBytes(_tripIdHashSize);
            length += bytes.Length;
            stream.Write(bytes, 0, bytes.Length);
            bytes = BitConverter.GetBytes(_tripIdLinkedListPointer);
            length += bytes.Length;
            stream.Write(bytes, 0, bytes.Length);
            bytes = BitConverter.GetBytes(_nextId);
            length += bytes.Length;
            stream.Write(bytes, 0, bytes.Length);

            // write attributes.
            length += _attributes.Serialize(stream);

            return length;
        }

        internal static TripsDb ReadFrom(Stream stream, uint id)
        {
            var buffer = new byte[4];

            var version = stream.ReadByte();
            if (version != 1) throw new InvalidDataException($"Cannot read {nameof(TripsDb)}, invalid version #.");

            // read data.
            var tripIds = MemoryArray<string>.CopyFromWithSize(stream);
            var tripAttributeIds = MemoryArray<uint>.CopyFromWithSize(stream);
            var tripIdPointersPerHash = MemoryArray<uint>.CopyFromWithSize(stream);
            var tripIdLinkedList = MemoryArray<uint>.CopyFromWithSize(stream);

            stream.Read(buffer, 0, 4);
            var tripIdHashSize = BitConverter.ToInt32(buffer, 0);
            stream.Read(buffer, 0, 4);
            var tripIdLinkedListPointer = BitConverter.ToUInt32(buffer, 0);
            stream.Read(buffer, 0, 4);
            var nextId = BitConverter.ToUInt32(buffer, 0);

            // read attributes.
            var attributes = AttributesIndex.Deserialize(stream, true);

            return new TripsDb(id, tripIds, tripAttributeIds, tripIdPointersPerHash, tripIdLinkedList, attributes,
                tripIdLinkedListPointer, nextId, tripIdHashSize);
        }

        /// <summary>
        /// Returns a deep in-memory copy.
        /// </summary>
        /// <returns></returns>
        public TripsDb Clone()
        {
            // it is up to the user to make sure not to clone when writing. 
            var tripIds = new MemoryArray<string>(_tripIds.Length);
            tripIds.CopyFrom(_tripIds, _tripIds.Length);
            var tripAttributeIds = new MemoryArray<uint>(_tripAttributeIds.Length);
            tripAttributeIds.CopyFrom(_tripAttributeIds, _tripAttributeIds.Length);
            var tripIdPointersPerHash = new MemoryArray<uint>(_tripIdPointersPerHash.Length);
            tripIdPointersPerHash.CopyFrom(_tripIdPointersPerHash, _tripIdPointersPerHash.Length);
            var tripIdLinkedList = new MemoryArray<uint>(_tripIdLinkedList.Length);
            tripIdLinkedList.CopyFrom(_tripIdLinkedList, _tripIdLinkedList.Length);

            // don't clone the attributes, it's supposed to be add-only anyway.
            // it's up to the user not to write to it from multiple threads.
            return new TripsDb(_dbId,
                tripIds, tripAttributeIds, tripIdPointersPerHash, tripIdLinkedList, _attributes,
                _tripIdLinkedListPointer, _nextId, _tripIdHashSize);
        }

        public bool Get(string globalId, Trip objectToWrite)
        {
            // The databases use an internal linked list
            // We calculate the initial pointer based on the hash...
            var hash = Hash(globalId);
            // and use the dictionary to get an initial pointer
            var pointer = _tripIdPointersPerHash[hash];
            while (pointer != _noData)
            {
                // The linked list is basically 
                // an array of [internalId, nextBucketPointer, internalId, nextBucketPointer, ... ]
                var internalId = _tripIdLinkedList[pointer + 0];
                if (Get(new TripId(_dbId, internalId), objectToWrite))
                {
                    var potentialMatch = _tripIds[internalId];
                    if (potentialMatch == globalId)
                    {
                        return true;
                    }
                }

                pointer = _tripIdLinkedList[pointer + 1];
            }

            return false;
        }

        public bool Get(TripId id, Trip objectToWrite)
        {
            if (id.DatabaseId != _dbId)
            {
                return false;
            }

            var internalId = id.InternalId;
            if (internalId >= _nextId)
            {
                return false;
            }

            objectToWrite.Id = id;
            objectToWrite.GlobalId = _tripIds[internalId];
            objectToWrite.Attributes = _attributes.Get(_tripAttributeIds[internalId]);

            return true;
        }
    }
}