using System;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Data.Walks
{
    using UnixTime =UInt32;
    using LocId = UInt64;
    
    
    /// <summary>
    /// The transfergenerator takes a journey and a next connection.
    /// Using those, it extends the journey if this is possible.
    /// </summary>
    public interface IOtherModeGenerator
    {
        
        
        
        /// <summary>
        /// Create a new journey, which extends 'buildOn' with 'nextConnection'
        /// This might return null if the transfer time is too short.
        /// This might involve querying for footpaths
        /// </summary>
        Journey<T> CreateDepartureTransfer<T>(StopsDb.StopsDbReader stopsDb, Journey<T> buildOn, ulong timeWhenLeaving, (uint, uint) otherLocation) where T : IJourneyStats<T>;


        /// <summary>
        /// Reverse add connection. Chains the transfer and connection to the given journey.
        /// However, this is the method to use for journeys which are built backwards in time 
        /// </summary>
        Journey<T> CreateArrivingTransfer<T>(StopsDb.StopsDbReader stopsDb, Journey<T> buildOn, ulong timeWhenDeparting, (uint, uint) otherLocation) where T : IJourneyStats<T>;

        float Range();


    }
}