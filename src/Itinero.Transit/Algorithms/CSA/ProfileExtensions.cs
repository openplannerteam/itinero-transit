using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Itinero.Transit.Data;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Algorithms.CSA
{
    public static class ProfileExtensions
    {
        /// <summary>
        /// Calculates the profiles Journeys for the given coordinates.
        ///
        /// Starts with an EAS, then gives profiled journeys.
        ///
        /// Note that the profile scan might scan in a window far smaller then the last-arrivaltime
        /// 
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="depLocation"></param>
        /// <param name="arrivalLocaiton"></param>
        /// <param name="startTime"></param>
        /// <param name="lastArrivalTime"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<Journey<T>> CalculateJourneys<T>
        (this Profile<T> profile, (uint, uint) depLocation, (uint, uint) arrivalLocaiton,
            ulong departureTime, ulong lastArrivalTime) where T : IJourneyStats<T>
        {
            var eas = new EarliestConnectionScan<T>(
                depLocation, arrivalLocaiton,
                departureTime, lastArrivalTime,
                profile
            );
            var earliestJourney = eas.CalculateJourney();
            return null; // TODO
        }
    }
}