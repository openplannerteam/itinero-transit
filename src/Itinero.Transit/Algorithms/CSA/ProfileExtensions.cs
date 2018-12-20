using System;
using System.Collections.Generic;
using Itinero.IO.LC;
using Itinero.Transit.Data;
using Itinero.Transit.Journeys;
using Itinero.Transit.Logging;

namespace Itinero.Transit.Algorithms.CSA
{
    public static class ProfileExtensions
    {

        public static IEnumerable<Journey<T>> CalculateJourneys<T>
        (this Profile<T> profile,string from, string to,
            DateTime departure, DateTime arrival) where T : IJourneyStats<T>
        {
            var reader = profile.StopsDb.GetReader();
            reader.MoveTo(from);
            var fromId = reader.Id;
            reader.MoveTo(to);
            var toId = reader.Id;
            return profile.CalculateJourneys(
                fromId, toId, departure.ToUnixTime(), arrival.ToUnixTime());
        }
        ///  <summary>
        ///  Calculates the profiles Journeys for the given coordinates.
        /// 
        ///  Starts with an EAS, then gives profiled journeys.
        /// 
        ///  Note that the profile scan might scan in a window far smaller then the last-arrivaltime
        ///  
        ///  </summary>
        ///  <param name="profile"></param>
        ///  <param name="depLocation"></param>
        ///  <param name="arrivalLocation"></param>
        /// <param name="departureTime"></param>
        /// <param name="lastArrivalTime"></param>
        ///  <typeparam name="T"></typeparam>
        ///  <returns></returns>
        public static IEnumerable<Journey<T>> CalculateJourneys<T>
        (this Profile<T> profile, (uint, uint) depLocation, (uint, uint) arrivalLocation,
            ulong departureTime, ulong lastArrivalTime) where T : IJourneyStats<T>
        {
            var eas = new EarliestConnectionScan<T>(
                depLocation, arrivalLocation,
                departureTime, lastArrivalTime,
                profile
            );
            var earliestJourney = eas.CalculateJourney((d, d0) => lastArrivalTime);
            if (earliestJourney == null)
            {
                Log.Information("Could not determine a route");
                return null;
            }

            var pcs = new ProfiledConnectionScan<T>(
                depLocation, arrivalLocation,
                departureTime, lastArrivalTime,
                profile,
                eas
            );

            return pcs.CalculateJourneys();
        }
    }
}