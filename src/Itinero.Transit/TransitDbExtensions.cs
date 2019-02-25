using System;
using System.Collections.Generic;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Algorithms.Search;
using Itinero.Transit.Data;
using Itinero.Transit.Journeys;

namespace Itinero.Transit
{
    /// <summary>
    /// Transit db extensions.
    /// </summary>
    public static class TransitDbExtensions
    {
        /// <summary>
        /// Finds the closest stop.
        /// </summary>
        /// <param name="snapShot">The snapshot.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <param name="maxDistanceInMeters">The maximum distance in meters.</param>
        /// <returns>The closest stop.</returns>
        public static IStop FindClosestStop(this TransitDb.TransitDbSnapShot snapShot, double longitude, double latitude, 
            double maxDistanceInMeters = 1000)
        {
            return snapShot.StopsDb.SearchClosest(longitude, latitude, maxDistanceInMeters);
        }

        /// <summary>
        /// Calculates all journeys between departure and arrival stop.
        /// </summary>
        /// <param name="snapshot">The transit db snapshot.</param>
        /// <param name="profile">The profile.</param>
        /// <param name="departureStop">The departure stop.</param>
        /// <param name="arrivalStop">The arrival stop.</param>
        /// <param name="departure">The departure time.</param>
        /// <param name="arrival">The arrival time.</param>
        /// <param name="lookAheadInSeconds">The look ahead window.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<Journey<T>> CalculateJourneys<T>(this TransitDb.TransitDbSnapShot snapshot,
            Profile<T> profile, (uint tileId, uint localId) departureStop, (uint tileId, uint localId) arrivalStop,
            DateTime? departure = null, DateTime? arrival = null, uint lookAheadInSeconds = 24 * 60 * 60)
                where T : IJourneyStats<T>
        {
            return snapshot.CalculateJourneys(profile, departureStop, arrivalStop,
                departure?.ToUnixTime() ?? 0, arrival?.ToUnixTime() ?? 0, lookAheadInSeconds);
        }
    }
}