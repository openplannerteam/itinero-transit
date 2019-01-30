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
        (this Profile<T> profile, string from, string to,
            DateTime? departure = null, DateTime? arrival = null) where T : IJourneyStats<T>
        {
            var reader = profile.TransitDbSnapShot.StopsDb.GetReader();
            reader.MoveTo(from);
            var fromId = reader.Id;
            reader.MoveTo(to);
            var toId = reader.Id;
            return profile.CalculateJourneys(
                fromId, toId,
                departure?.ToUnixTime() ?? 0,
                arrival?.ToUnixTime() ?? 0);
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
            ulong departureTime = 0, ulong lastArrivalTime = 0) where T : IJourneyStats<T>
        {
            if (departureTime == 0 && lastArrivalTime == 0)
            {
                throw new ArgumentException("At least one of departure or arrival time should be given");
            }


            IConnectionFilter filter = null;
            if (departureTime == 0)
            {
                var las = new LatestConnectionScan<T>(depLocation, arrivalLocation,
                    lastArrivalTime - 24 * 60 * 60, lastArrivalTime,
                    profile);
                var lasJourney = las.CalculateJourney(
                    (journeyArr, journeyDep) =>
                    {
                        var diff = journeyArr - journeyDep;
                        return journeyArr - diff;
                    });
                var lasTime = lasJourney.ArrivalTime() - lasJourney.Root.DepartureTime();
                departureTime = lasJourney.Root.DepartureTime() - lasTime;
                lastArrivalTime = lasJourney.ArrivalTime();
                filter = las;
            }

            bool lastArrivalTimeSet = lastArrivalTime != 0;
            if (!lastArrivalTimeSet)
            {
                lastArrivalTime = departureTime + 24 * 60 * 60;
            }


            var eas = new EarliestConnectionScan<T>(
                depLocation, arrivalLocation,
                departureTime, lastArrivalTime,
                profile
            );
            var time = lastArrivalTime;
            var earliestJourney = eas.CalculateJourney(
                (journeyDep, journeyArr) => 
                    lastArrivalTimeSet ? time :
                    journeyArr + (journeyArr - journeyDep));
            
            if (earliestJourney == null)
            {
                Log.Information("Could not determine a route");
                return null;
            }

            departureTime = earliestJourney.Root.DepartureTime();
            if (!lastArrivalTimeSet)
            {
                lastArrivalTime = earliestJourney.ArrivalTime() +
                                  (earliestJourney.ArrivalTime() - earliestJourney.Root.DepartureTime());
            }
            if (filter == null)
            {
                filter = eas;
            }
            else
            {
                filter = new DoubleFilter(eas, filter);
            }

            var pcs = new ProfiledConnectionScan<T>(
                depLocation, arrivalLocation,
                departureTime, lastArrivalTime,
                profile,
                filter
            );

            return pcs.CalculateJourneys();
        }
    }
}