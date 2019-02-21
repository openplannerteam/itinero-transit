using System;
using System.Runtime.InteropServices;
using Itinero.Transit.Data;

// ReSharper disable BuiltInTypeReferenceStyle

namespace Itinero.Transit.Journeys
{
    using TimeSpan = UInt16;
    using UnixTime = UInt64;

    //using LocId = UInt64;


    /// <summary>
    /// A journey is a part in an intermodal trip, describing the route the user takes.
    ///
    /// Normally, a journey is constructed with the start location hidden the deepest in the data structure.
    /// The Time is mostly the arrival time.
    ///
    /// The above properties are reversed in the CPS algorithm. The last step of that algorithm is to reverse the journeys,
    /// so that users of the lib get a uniform experience
    /// </summary>
    public class Journey<T>
        where T : IJourneyStats<T>
    {
        public static readonly Journey<T> InfiniteJourney = new Journey<T>();

        public static readonly Journey<T> NegativeInfiniteJourney
            = new Journey<T>(UnixTime.MinValue);

        /// <summary>
        /// The first link of the journey. Can be useful when in need of the real departure time
        /// </summary>
        public readonly Journey<T> Root;


        /// <summary>
        /// The previous link in this journey. Can be null if this is where we start the journey
        /// </summary>
        public readonly Journey<T> PreviousLink;

        /// <summary>
        /// Sometimes, we encounter two subjourneys which are equally optimal.
        /// Instead of duplicating them across the graph, we have this special journey part which gives an alternative version split
        /// </summary>
        public readonly Journey<T> AlternativePreviousLink;

        /// <summary>
        /// Indicates that this journeyPart is not a simple PT-connection,
        /// but rather something as a walk, transfer, ...
        /// </summary>
        public readonly bool SpecialConnection;

        /// <summary>
        /// The connection id, taken in this last part of this journey
        /// We resort to magic for special connections (if Special Connection is set), such as walks between stops
        ///
        /// 1 This is the Genesis Connection
        /// 2: This is a Transfer (within the same station)
        /// 3: This is a Walk, from the previous journey collection to here.
        ///         Note that the actual route is _not_ saved as not to use too much memory
        /// 
        /// </summary>
        public readonly uint Connection;

        /// <summary>
        /// Constant indicating that the journey starts here
        /// </summary>
// ReSharper disable once InconsistentNaming
        public const uint GENESIS = 1;

        /// <summary>
        /// Constant indicating that the traveller doesn't move, but waits or changes platform
        /// Also used as filler between the genesis and first departure
        /// </summary>
// ReSharper disable once MemberCanBePrivate.Global
// ReSharper disable once InconsistentNaming
        public const uint TRANSFER = 2;

        // ReSharper disable once InconsistentNaming
        public const uint WALK = 3;

        /// <summary>
        /// Indicates that this journey represents a choice
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public const uint JOINED_JOURNEYS = 4;

        /// <summary>
        /// Keep track of Location.
        /// In normal circumstances, this is the location where the journey arrives
        /// (also in the cases for transfers, walks, ...)
        /// 
        /// Only for the genesis connection, this is the departure location.
        /// </summary>
        public readonly (uint tileId, uint localId) Location;

        /// <summary>
        /// Keep track of Time.
        /// In normal circumstances, this is the time when the journey arrives
        /// (also in the cases for transfers, walks, ...)
        /// 
        /// Only for the genesis connection, this is the departure time.
        /// </summary>
        public readonly UnixTime Time;

        /// <summary>
        /// The trip id of the connection
        /// This is used as a freeform debugging tag in the Genesis Element
        /// </summary>
        public readonly uint TripId;

        public const int EarliestArrivalScanJourney = 1;
        public const int LatestArrivalScanJourney = 2;
        public const int ProfiledScanJourney = 3;


        /// <summary>
        /// Keeps some statistics about the journey
        /// </summary>
        public readonly T Stats;

        /// <summary>
        /// Hashcode is calculated at the start for quick comparisons
        /// </summary>
        private readonly int _hashCode;

        /// <summary>
        /// Infinity constructor
        /// This is the constructor which creates a journey representing an infinite journey.
        /// There is a singleton available: Journey.InfiniteJourney.
        /// This object is used when needing a dummy object to compare to, e.g. as journey to locations that can't be reached
        /// </summary>
        private Journey(UnixTime time = UnixTime.MaxValue)
        {
            Root = this;
            PreviousLink = this;
            Connection = int.MaxValue;
            Location = (uint.MaxValue, uint.MaxValue);
            Time = time;
            SpecialConnection = true;
            TripId = uint.MaxValue;
            _hashCode = CalculateHashCode();
        }

        /// <summary>
        /// All-exposing constructor. I like the 'readonly' aspect of code.
        /// Note that Stats will not be saved, but be used as constructor with 'stats.add(this)' is
        /// </summary>
        /// <param name="root"></param>
        /// <param name="previousLink"></param>
        /// <param name="specialLink"></param>
        /// <param name="connection"></param>
        /// <param name="location"></param>
        /// <param name="time"></param>
        /// <param name="tripId"></param>
        /// <param name="stats"></param>
        private Journey(Journey<T> root, Journey<T> previousLink, bool specialLink, uint connection,
            (uint localTileId, uint localId) location, UnixTime time, uint tripId, T stats)
        {
            Root = root;
            SpecialConnection = specialLink;
            PreviousLink = previousLink;
            Connection = connection;
            Location = location;
            Time = time;
            TripId = tripId;
            Stats = stats.Add(this);
            _hashCode = CalculateHashCode();
        }

        /// <summary>
        /// Genesis constructor.
        /// This constructor creates a root journey
        /// </summary>
        public Journey((uint localTileId, uint localId) location, UnixTime departureTime, T initialStats,
            uint debuggingFreeformTag = 0)
        {
            Root = this;
            PreviousLink = null;
            Connection = GENESIS;
            SpecialConnection = true;
            Location = location;
            Time = departureTime;
            Stats = initialStats;
            TripId = debuggingFreeformTag;
            _hashCode = CalculateHashCode();
        }


        public Journey(Journey<T> optionA, Journey<T> optionB)
        {
            if (optionA == null)
            {
                throw new ArgumentNullException("optionA");
            }

            if (optionB == null)
            {
                throw new ArgumentNullException("optionB");
            }

            AlternativePreviousLink = optionB;
            // Option A is seen as the 'true' predecessor'
            Root = optionA.Root;
            PreviousLink = optionA;
            Connection = JOINED_JOURNEYS;
            SpecialConnection = true;
            Location = optionA.Location;
            Time = optionA.Time;
            Stats = optionA.Stats;
            TripId = optionA.Root.TripId;
            _hashCode = optionA._hashCode + optionB._hashCode;
        }

        /// <summary>
        /// Chaining constructor
        /// Gives a new journey which extends this journey with the given connection.
        /// </summary>
        internal Journey<T> Chain(uint connection, UnixTime arrivalTime, (uint localTileId, uint localId) location,
            uint tripId)
        {
            return new Journey<T>(
                Root, this, false, connection, location, arrivalTime, tripId, Stats);
        }

        public Journey<T> ChainForward(IConnection c)
        {
            if (SpecialConnection && Connection == GENESIS)
            {
                // We do something special here:
                // We move the genesis to the departure time of the connection
                // This is used by EAS to have a correcter departure time
                var newGenesis = new Journey<T>(Location, c.DepartureTime, Stats.EmptyStat(), TripId);
                return newGenesis.Chain(c.Id, c.ArrivalTime, c.ArrivalStop, c.TripId);
            }

            return Chain(c.Id, c.ArrivalTime, c.ArrivalStop, c.TripId);
        }

        public Journey<T> ChainBackward(IConnection c)
        {
            if (SpecialConnection && Connection == GENESIS)
            {
                // We do something special here:
                // We move the genesis to the departure time of the connection
                var newGenesis = new Journey<T>(Location, c.ArrivalTime, Stats.EmptyStat(), TripId);
                return newGenesis.Chain(c.Id, c.DepartureTime, c.DepartureStop, c.TripId);
            }

            return Chain(c.Id, c.DepartureTime, c.DepartureStop, c.TripId);
        }


        /// <summary>
        /// Chaining constructorChain
        /// Gives a new journey which extends this journey with the given connection.
        /// </summary>
        public Journey<T> ChainSpecial(uint specialCode, UnixTime arrivalTime,
            (uint localTileId, uint localId) location, uint tripId)
        {
            return new Journey<T>(
                Root, this, true, specialCode, location, arrivalTime, tripId, Stats);
        }

        /// <summary>
        /// Extends this journey with the given connection. If there is waiting time between this journey and the next,
        /// a 'Transfer' link is included.
        /// Transfer links _should not_ be used to calculate the number of transfers, the differences in trip-ids should be used for this! 
        /// </summary>
        public Journey<T> Transfer(UnixTime departureTime)
        {
            // Creating the transfer
            return new Journey<T>(
                // ReSharper disable once ArrangeThisQualifier
                Root, this, true, TRANSFER, this.Location, departureTime, uint.MaxValue, Stats);
        }

        public Journey<T> TransferForward(IConnection c)
        {
            if (Time == c.DepartureTime)
            {
                // No transfer needed: seamless link
                return Chain(c.Id, c.ArrivalTime, c.ArrivalStop, c.TripId);
            }

            return Transfer(c.DepartureTime)
                .Chain(c.Id, c.ArrivalTime, c.ArrivalStop, c.TripId);
        }


        /// <summary>
        /// Returns the trip id of the most recent connection which has a valid trip.
        /// </summary>
        public uint? LastTripId()
        {
            return SpecialConnection ? PreviousLink?.LastTripId() : TripId;
        }

        /// <summary>
        /// Departure time of this journey part
        /// </summary>
        /// <returns></returns>
        public UnixTime DepartureTime()
        {
            if (SpecialConnection && Connection == GENESIS)
            {
                return Time;
            }

            return PreviousLink.Time;
        }

        /// <summary>
        /// Arrival time of this journey part
        /// </summary>
        /// <returns></returns>
        public UnixTime ArrivalTime()
        {
            return Time;
        }

        public override string ToString()
        {
            return ToString((StopsDb.StopsDbReader) null);
        }

        public string ToString(StopsDb db)
        {
            return ToString(db.GetReader());
        }

        public string ToString(StopsDb.StopsDbReader reader, int maxDepth = 50)
        {
            if (maxDepth == 0)
            {
                return "... More connections omitted, journey maxDepth has been reached ...";
            }
            
            var previous = "";
            if (PreviousLink != null && !ReferenceEquals(PreviousLink, this))
            {
                previous = PreviousLink.ToString(reader, maxDepth - 1);
            }

            return $"{previous}\n  {PartToString(reader)}\n    {Stats} (Trip {TripId})";
        }

        private string PartToString(StopsDb.StopsDbReader reader)
        {
            reader?.MoveTo(Location);
            var location = Location.ToString();
            if (reader?.Attributes != null)
            {
                location = reader.Attributes.ToString();
            }

            if (!SpecialConnection)
                return
                    $"Connection {Connection} to {location}, arriving at {DateTimeExtensions.FromUnixTime(Time):yyyy-MM-dd HH:mm}";

            switch (Connection)
            {
                case GENESIS:
                    var freeForm = ", debugging free form tag is ";
                    switch (TripId)
                    {
                        case EarliestArrivalScanJourney:
                            freeForm += "EAS";
                            break;
                        case LatestArrivalScanJourney:
                            freeForm += "LAS";
                            break;
                        case ProfiledScanJourney:
                            freeForm += "PCS";
                            break;
                        case uint.MaxValue:
                            freeForm = "";
                            break;
                        default:
                            freeForm += $"{TripId}";
                            break;
                    }


                    return
                        $"Genesis at {location}, time is {Time.FromUnixTime():HH:mm}{freeForm}";
                case WALK:
                    return
                        $"Walk to {location} in {Time - PreviousLink.Time} till {Time.FromUnixTime():HH:mm} seconds";
                case TRANSFER:
                    return
                        $"Transfer/Wait for {Time - PreviousLink.Time} seconds till {Time.FromUnixTime():HH:mm} in {location}";
                case JOINED_JOURNEYS:
                    return
                        $"Choose a journey: there is a equivalent journey available. Continuing print via one arbitrary option";

                case int.MaxValue:
                    return "Infinite journey";
                default:
                    throw new ArgumentException($"Unknown Special Connection code {Connection}");
            }
        }


        protected bool Equals(Journey<T> other)
        {
            return _hashCode == other._hashCode;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Journey<T>) obj);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        private int CalculateHashCode()
        {
            unchecked
            {
                var hashCode = SpecialConnection.GetHashCode();
                hashCode = (hashCode * 397) ^ (PreviousLink?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (int) Connection;
                hashCode = (hashCode * 397) ^ Location.GetHashCode();
                hashCode = (hashCode * 397) ^ Time.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) TripId;
                return hashCode;
            }
        }
    }
}