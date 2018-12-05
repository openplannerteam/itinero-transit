using System;
using Itinero.Transit.Data;

// ReSharper disable BuiltInTypeReferenceStyle

namespace Itinero.Transit
{
    using TimeSpan = UInt16;
    using UnixTime = UInt64;
    using LocId = UInt64;


    /// <summary>
    /// A journey is a part in an intermodal trip, describing the route the user takes.
    ///
    /// Normally, a journey is constructed with the start location hidden the deepest in the data structure.
    /// The Time is mostly the arrival time.
    ///
    /// The above properties are reversed in the CPS algorithm. The last step of that algorithm is to reverse the journeys,
    /// so that users of the lib get a uniform experience
    /// </summary>
    public class Journey<T> where T : IJourneyStats<T>
    {
        public static readonly Journey<T> InfiniteJourney = new Journey<T>();

        /// <summary>
        /// The first link of the journey. Can be useful when in need of the real departure time
        /// </summary>
        public readonly Journey<T> Root;


        /// <summary>
        /// The previous link in this journey. Can be null if this is where we start the journey
        /// </summary>
        public readonly Journey<T> PreviousLink;

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
        /// Keep track of Location.
        /// In normal circumstances, this is the location where the journey arrives
        /// (also in the cases for transfers, walks, ...)
        /// 
        /// Only for the genesis connection, this is the departure location.
        /// </summary>
        public readonly LocId Location;

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
        /// </summary>
        public readonly uint TripId;


        /// <summary>
        /// Keeps some statistics about the journey
        /// </summary>
        public readonly T Stats;

        /// <summary>
        /// Infinity constructor
        /// This is the constructor which creates a journey representing an infinite journey.
        /// There is a singleton availabe: Journey.InfiniteJourney.
        /// This object is used when needing a dummy object to compare to, e.g. as journey to locations that can't be reached
        /// </summary>
        private Journey()
        {
            Root = this;
            PreviousLink = this;
            Connection = int.MaxValue;
            Location = LocId.MaxValue;
            Time = UnixTime.MaxValue;
            SpecialConnection = true;
            TripId = uint.MaxValue;
        }

        /// <summary>
        /// All-exposing constructor. I like the 'readonly' aspect of code.
        /// Note that Stats will not be saved, but be used as constructor with 'stats.add(this)' is
        /// </summary>
        /// <param name="root"></param>
        /// <param name="previousLink"></param>
        /// <param name="connection"></param>
        /// <param name="location"></param>
        /// <param name="time"></param>
        /// <param name="tripId"></param>
        /// <param name="stats"></param>
        private Journey(Journey<T> root, Journey<T> previousLink, bool specialLink, uint connection,
            LocId location, UnixTime time, uint tripId, T stats)
        {
            Root = root;
            SpecialConnection = specialLink;
            PreviousLink = previousLink;
            Connection = connection;
            Location = location;
            Time = time;
            TripId = tripId;
            Stats = stats.Add(this);
        }

        /// <summary>
        /// Genesis constructor.
        /// This constructor creates a root journey
        /// </summary>
        public Journey(LocId location, UnixTime departureTime, T statsFactory)
        {
            Root = this;
            PreviousLink = null;
            Connection = GENESIS;
            SpecialConnection = true;
            Location = location;
            Time = departureTime;
            Stats = statsFactory;
            TripId = uint.MaxValue;
        }

        /// <summary>
        /// Chaining constructor
        /// Gives a new journey which extends this journey with the given connection.
        /// </summary>
        public Journey<T> Chain(uint connection, UnixTime arrivalTime, LocId location, uint TripId)
        {
            return new Journey<T>(
                Root, this, false, connection, location, arrivalTime, TripId, Stats);
        }

        public Journey<T> ChainForward(Connection c)
        {
            return Chain(c.Id, c.ArrivalTime, c.ArrivalLocation, c.TripId);
        }
        
        public Journey<T> ChainBackward(Connection c)
        {
            return Chain(c.Id, c.DepartureTime, c.DepartureLocation, c.TripId);
        }

        
        /// <summary>
        /// Chaining constructor
        /// Gives a new journey which extends this journey with the given connection.
        /// </summary>
        public Journey<T> ChainSpecial(uint specialCode, UnixTime arrivalTime, LocId location)
        {
            return new Journey<T>(
                Root, this, true, specialCode, location, arrivalTime, uint.MaxValue, Stats);
        }

        /// <summary>
        /// Extends this journey with the given connection. If there is waiting time between this journey and the next,
        /// a 'Transfer' link is included.
        /// Transfer links _should not_ be used to calculate the number of transfers, the differences in trip-ids should be used for this! 
        /// </summary>
        /// <param name="connection"></param>
        public Journey<T> Transfer(uint connection, UnixTime departureTime, UnixTime arrivalTime, LocId arrivalLocation, uint tripId)
        {
            if (Time == departureTime)
            {
                // No transfer needed: seamless link
                return Chain(connection, arrivalTime, arrivalLocation, tripId);
            }

            // We have to create the transfer. Lets create that
            var transfer = new Journey<T>(
                // ReSharper disable once ArrangeThisQualifier
                Root, this, true, TRANSFER, this.Location, departureTime, uint.MaxValue, Stats);
            return transfer.Chain(connection, arrivalTime, arrivalLocation, tripId);
        }

        public Journey<T> TransferForward(Connection c)
        {
            return Transfer(c.Id, c.DepartureTime, c.ArrivalTime, c.ArrivalLocation, c.TripId);
        }


        /// <summary>
        /// Returns the trip id of the most recent connection which has a valid trip.
        /// </summary>
        public uint? LastTripId()
        {
            return SpecialConnection ? PreviousLink?.LastTripId() : TripId;
        }

        public UnixTime StartTime()
        {
            if (SpecialConnection && Connection == GENESIS)
            {
                return Time;
            }

            return PreviousLink.Time;
        }
    }
}