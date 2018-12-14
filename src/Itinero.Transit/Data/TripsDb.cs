using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Transit.Data.Attributes;
using Reminiscence.Arrays;

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
        private const uint NoData = uint.MaxValue;
        private readonly ArrayBase<uint> _tripIdPointersPerHash;
        private uint _tripIdLinkedListPointer = 0;
        private readonly ArrayBase<uint> _tripIdLinkedList;
        
        private readonly AttributesIndex _attributes;
        private uint _nextId = 0;
        
        /// <summary>
        /// Creates a new trips database.
        /// </summary>
        public TripsDb()
        {
            _tripIds = new MemoryArray<string>(0);
            _tripAttributeIds = new MemoryArray<uint>(0);
            _tripIdPointersPerHash = new MemoryArray<uint>(_tripIdHashSize);
            for (var h = 0; h < _tripIdPointersPerHash.Length; h++)
            {
                _tripIdPointersPerHash[h] = NoData;
            }
            _tripIdLinkedList = new MemoryArray<uint>(0);
            _attributes = new AttributesIndex();
        }

        /// <summary>
        /// Adds a new trip.
        /// </summary>
        /// <param name="globalId">The global id.</param>
        /// <param name="attributes">The attributes.</param>
        /// <returns>The trip id.</returns>
        public uint Add(string globalId, IEnumerable<Attribute> attributes = null)
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

            return tripId;
        }

        private uint Hash(string id)
        { // https://stackoverflow.com/questions/5154970/how-do-i-create-a-hashcode-in-net-c-for-a-string-that-is-safe-to-store-in-a
            unchecked
            {
                var hash = (uint) 23;
                foreach (var c in id)
                {
                    hash = hash * 31 + c;
                }

                return  (uint) (hash % _tripIdHashSize);
            }
        }

        /// <summary>
        /// Gets the reader.
        /// </summary>
        /// <returns>The reader.</returns>
        public TripsDbReader GetReader()
        {
            return new TripsDbReader(this);
        }

        /// <summary>
        /// A trips reader.
        /// </summary>
        public class TripsDbReader : ITrip
        {
            private readonly TripsDb _tripsDb;
            
            internal TripsDbReader(TripsDb tripsDb)
            {
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
            /// Moves this enumerator to the trip with the given global id.
            /// </summary>
            /// <param name="globalId">The global id.</param>
            /// <returns>True if the trip was found and there is data.</returns>
            public bool MoveTo(string globalId)
            {
                var hash = _tripsDb.Hash(globalId);
                var pointer = _tripsDb._tripIdPointersPerHash[hash];
                while (pointer != NoData)
                {
                    var tripId = _tripsDb._tripIdLinkedList[pointer + 0];

                    if (this.MoveTo(tripId))
                    {
                        var potentialMatch = this.GlobalId;
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

            /// <summary>
            /// Gets the attributes.
            /// </summary>
            public IAttributeCollection Attributes => _tripsDb._attributes.Get(_tripsDb._tripAttributeIds[_tripId]);

            /// <summary>
            /// Gets the id.
            /// </summary>
            public uint Id => _tripId;

            /// <summary>
            /// Gets the global id.
            /// </summary>
            public string GlobalId => _tripsDb._tripIds[_tripId];
        }
    }
}