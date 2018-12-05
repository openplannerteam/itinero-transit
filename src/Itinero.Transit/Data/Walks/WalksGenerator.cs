using System;
using System.Collections.Generic;
using OsmSharp.API;

namespace Itinero.Transit.Data.Walks
{
    using UnixTime =UInt32;
    using LocId = UInt64;
    
    
    /// <summary>
    /// The transfergenerator takes a journey and a next connection.
    /// Using those, it extends the journey if this is possible.
    /// </summary>
    public interface WalksGenerator
    {
        /// <summary>
        /// Create a new journey, which extends 'buildOn' with 'nextConnection'
        /// This might return null if the transfer time is too short.
        /// This might involve querying for footpaths
        /// </summary>
        Journey<T> CreateDepartureTransfer<T>(Journey<T> buildOn, Connection c) where T : IJourneyStats<T>;


        /// <summary>
        /// Reverse add connection. Chains the transfer and connection to the given journey.
        /// However, this is the method to use for journeys which are built backwards in time 
        /// </summary>
        Journey<T> CreateArrivingTransfer<T>(Journey<T> buildOn,Connection c) where T : IJourneyStats<T>;

    }
}