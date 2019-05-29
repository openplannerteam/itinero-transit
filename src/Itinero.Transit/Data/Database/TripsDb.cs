using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Itinero.Transit.Data.Attributes;
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
    public class TripsDb
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

        /// <summary>d
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

        /// <summary>
        /// Gets the reader.
        /// </summary>
        /// <returns>The reader.</returns>
        public TripsDbReader GetReader()
        {
            return new TripsDbReader(_dbId, this);
        }

        /// <summary>
        /// A trips reader.
        /// </summary>
        public class TripsDbReader : ITripReader
        {
            private readonly uint _dbId;
            private readonly TripsDb _tripsDb;

            internal TripsDbReader(uint dbId, TripsDb tripsDb)
            {
                _dbId = dbId;
                _tripsDb = tripsDb;
            }

            private uint _tripId = uint.MaxValue;

            /// <summary>
            /// Moves this enumerator to the given trip.
            /// </summary>
            /// <param name="trip">The trip.</param>
            /// <returns>True if there is more data.</returns>
            public bool MoveTo(uint trip)
            {
                if (trip >= _tripsDb._nextId) return false;

                _tripId = trip;
                return true;
            }

            /// <summary>
            /// Moves this enumerator to the given trip.
            /// </summary>
            /// <param name="trip">The trip.</param>
            /// <returns>True if there is more data.</returns>
            public bool MoveTo(TripId trip)
            {
                // ReSharper disable once ConvertIfStatementToReturnStatement
                if (trip.DatabaseId != _dbId)
                {
                    return false;
                }

                return MoveTo(trip.InternalId);
            }

            /// <summary>
            /// Moves this enumerator to the trip with the given global id.
            /// </summary>
            /// <param name="globalId">The global id.</param>
            /// <returns>True if the trip was found and there is data.</returns>
            public bool MoveTo(string globalId)
            {
                var hash = _tripsDb.Hash(globalId);
                var pointer = _tripsDb._tripIdPointersPerHash[hash];
                while (pointer != _noData)
                {
                    var tripId = _tripsDb._tripIdLinkedList[pointer + 0];

                    if (MoveTo(tripId))
                    {
                        var potentialMatch = GlobalId;
                        if (potentialMatch == globalId)
                        {
                            _tripId = tripId;
                            return true;
                        }
                    }

                    pointer = _tripsDb._tripIdLinkedList[pointer + 1];
                }

                return false;
            }

            /// <inheritdoc />
            public IAttributeCollection Attributes => _tripsDb._attributes.Get(_tripsDb._tripAttributeIds[_tripId]);

            /// <inheritdoc />
            public TripId Id => new TripId(_dbId, _tripId);

            /// <inheritdoc />
            public string GlobalId => _tripsDb._tripIds[_tripId];
        }
    }
}