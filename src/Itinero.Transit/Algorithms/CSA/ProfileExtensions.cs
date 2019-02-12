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


        /// <summary>
        /// Calculates all journeys which depart at 'from' at the given departure time.
        ///
        /// Performs an Earliest Arrival Scan till as long as 'lastArrival' is not passed.
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="from"></param>
        /// <param name="departure"></param>
        /// <param name="lastArrival"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IReadOnlyDictionary<(uint localTileId, uint localId), Journey<T>> Isochrone<T>
            (this Profile<T> profile, string from, DateTime departure, DateTime lastArrival)
            where T : IJourneyStats<T>
        {

            profile = profile.LoadWindow(departure, lastArrival);
            
            var reader = profile.TransitDbSnapShot.StopsDb.GetReader();
            if (!reader.MoveTo(from)) throw new ArgumentException($"Departure location {from} was not found");
            var fromId = reader.Id;

            
            /*
             * We construct an Earliest Connection Scan.
             * A bit peculiar: there is _no_ arrival station specified.
             * This will cause EAS to scan all connections until 'lastArrival' has been reached;
             * to conclude that 'no journey to any of the specified arrival stations was found'.
             *
             * EAS.calculateJourneys will thus be null - but meanwhile every reachable station will be marked.
             * And it is exactly that which we need!
             */
            var eas = new EarliestConnectionScan<T>(
                new List<(uint localTileId, uint localId)>{fromId},
                new List<(uint localTileId, uint localId)>{}, 
                departure.ToUnixTime(), lastArrival.ToUnixTime(),
                profile
            );
            eas.CalculateJourney();

            return eas.GetAllJourneys();
        }
        
        /// <summary>
        /// Calculates all journeys which arrive at 'to' at last at the given arrival time.
        ///
        /// Performs a Latest Arrival Scan till as long as 'lastArrival' is not passed.
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="from"></param>
        /// <param name="departure"></param>
        /// <param name="lastArrival"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IReadOnlyDictionary<(uint localTileId, uint localId), Journey<T>> IsochroneLatestArrival<T>
            (this Profile<T> profile, string to, DateTime departure, DateTime lastArrival)
            where T : IJourneyStats<T>
        {

            profile = profile.LoadWindow(departure, lastArrival);
            
            var reader = profile.TransitDbSnapShot.StopsDb.GetReader();
            if (!reader.MoveTo(to)) throw new ArgumentException($"Departure location {to} was not found");
            var toId = reader.Id;

            
            /*
             * Same principle as the other IsochroneFunction
             */
            var las = new LatestConnectionScan<T>(
                new List<(uint localTileId, uint localId)>{},
                new List<(uint localTileId, uint localId)>{toId}, 
                departure.ToUnixTime(), lastArrival.ToUnixTime(),
                profile
            );
            las.CalculateJourney();

            return las.GetAllJourneys();
        }
        
        /// <summary>
        ///
        /// Calculates the profiles Journeys for the given coordinates.
        /// 
        ///  Starts with an EAS, then gives profiled journeys.
        /// 
        ///  Note that the profile scan might scan in a window far smaller then the last-arrivaltime
        /// 
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="departure"></param>
        /// <param name="arrival"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static IEnumerable<Journey<T>> CalculateJourneys<T>
        (this Profile<T> profile, string from, string to,
            DateTime? departure = null, DateTime? arrival = null) where T : IJourneyStats<T>
        {
            var reader = profile.TransitDbSnapShot.StopsDb.GetReader();
            if (!reader.MoveTo(from)) throw new ArgumentException($"Departure location {from} was not found");
            var fromId = reader.Id;
            if (!reader.MoveTo(to)) throw new ArgumentException($"Arrival location {to} was not found");
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

            if (depLocation == arrivalLocation)
            {
                throw new ArgumentException("Departure and arrival location are the same");
            }

            IConnectionFilter filter = null;
            if (departureTime == 0)
            {
                profile = profile.LoadWindow((lastArrivalTime - 24 * 60 * 60).FromUnixTime(),
                    lastArrivalTime.FromUnixTime());
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

            var lastArrivalTimeSet = lastArrivalTime != 0;
            if (!lastArrivalTimeSet)
            {
                lastArrivalTime = departureTime + 24 * 60 * 60;
            }

            // Make sure that enough entries are loaded
            profile = profile.LoadWindow(departureTime.FromUnixTime(), lastArrivalTime.FromUnixTime());
            var eas = new EarliestConnectionScan<T>(
                depLocation, arrivalLocation,
                departureTime, lastArrivalTime,
                profile
            );
            var time = lastArrivalTime;
            var earliestJourney = eas.CalculateJourney(
                (journeyDep, journeyArr) =>
                    lastArrivalTimeSet ? time : journeyArr + (journeyArr - journeyDep));

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