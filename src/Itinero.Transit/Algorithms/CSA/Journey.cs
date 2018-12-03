using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Serilog;
// ReSharper disable BuiltInTypeReferenceStyle

namespace Itinero.Transit
{
    
        
    using TimeSpan = UInt16;
    using Time = UInt32;
    using Id = UInt32;


    
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
        public readonly bool SpecialConnection = false;

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

        public const uint GENESIS = 1;
        public const uint TRANSFER = 2;
        public const uint WALK = 3;

        /// <summary>
        /// Keep track of Location.
        /// In normal circumstances, this is the location where the journey arrives
        /// (also in the cases for transfers, walks, ...)
        /// 
        /// Only for the genesis connection, this is the departure location.
        /// </summary>
        public readonly Id Location;

        /// <summary>
        /// Keep track of Time.
        /// In normal circumstances, this is the time when the journey arrives
        /// (also in the cases for transfers, walks, ...)
        /// 
        /// Only for the genesis connection, this is the departure time.
        /// </summary>
        public readonly Time Time;


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
            Location = int.MaxValue;
            Time = Time.MaxValue;
        }


        /// <summary>
        /// Genesis constructor.
        /// This constructor creates a root journey
        /// </summary>
        public Journey(Id location, Time departureTime, T statsFactory)
        {
            Root = this;
            PreviousLink = null;
            Connection = GENESIS;
            SpecialConnection = true;
            Location = location;
            Time = departureTime;
            Stats = statsFactory;
        }

        /// <summary>
        /// Chaining constructor
        /// Takes a previous journey and a current connection to build onto
        /// </summary>
        public Journey(Journey<T> previousLink, uint connection, Time arrivalTime, Id location)
        {
            Root = previousLink.Root;
            PreviousLink = previousLink;
            Connection = connection;
            Time = arrivalTime;
            Location = location;

            // Stats will read out the value from this journey. It is thus important to create it as last element
            Stats = previousLink.Stats.Add(this);
        }


        /// <summary>
        /// Returns the trip id of the most recent connection which has a valid trip.
        /// </summary>
        public uint? LastTripId(ConnectionsDb db)
        {
            return SpecialConnection ? PreviousLink?.LastTripId(db) : db.GetTripId(Connection);
        }
    }
}