using System;
using System.Collections.Generic;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Algorithms.Search;
using Itinero.Transit.Data;
using Itinero.Transit.Journeys;

namespace Itinero.Transit
{
    /// <summary>
    /// This class is the main entry point to request routes as library user.
    /// It exposes all the core algorithms in a consistent and easy to use way.
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
        public static IStop FindClosestStop(this TransitDb.TransitDbSnapShot snapShot, double longitude,
            double latitude,
            double maxDistanceInMeters = 1000)
        {
            return snapShot.StopsDb.SearchClosest(longitude, latitude, maxDistanceInMeters);
        }

        public static (uint, uint) FindStop(this TransitDb.TransitDbSnapShot snapshot, string locationId,
            string errMsg = null)
        {
            return snapshot.StopsDb.GetReader().FindStop(locationId, errMsg);
        }


        ///  <summary>
        ///  Calculates the earliest arriving journey which depart at 'from' at the given departure time and arrives at 'to'.
        /// 
        ///  Performs an Earliest Arrival Scan
        ///
        /// </summary>
        /// <param name="snapshot">The transit DB containing the PT-data</param>
        /// <param name="profile">The travellers' preferences</param>
        /// <param name="from">Where the traveller starts</param>
        /// <param name="to">WHere the traveller wishes to go to</param>
        /// <param name="departure">When the traveller would like to depart</param>
        /// <param name="lastArrival">When the traveller would like to arrive</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>A journey which is guaranteed to arrive as early as possible (or null if none was found)</returns>
        // ReSharper disable once UnusedMember.Global
        public static Journey<T> EarliestArrival<T>
        (this TransitDb.TransitDbSnapShot snapshot,
            Profile<T> profile,
            string from, string to,
            DateTime departure, DateTime lastArrival)
            where T : IJourneyStats<T>
        {
            var reader = snapshot.StopsDb.GetReader();
            var fromId = reader.FindStop(from, $"Departure location {from} was not found");
            var toId = reader.FindStop(to, $"Departure location {to} was not found");

            if (fromId.Equals(toId))
            {
                throw new ArgumentException($"The departure and arrival arguments are the same ({from})");
            }

            var eas = new EarliestConnectionScan<T>(snapshot,
                new List<(uint localTileId, uint localId)> {fromId},
                new List<(uint localTileId, uint localId)> {toId},
                departure.ToUnixTime(), lastArrival.ToUnixTime(),
                profile
            );
            return eas.CalculateJourney();
        }

        ///  <summary>
        ///  Calculates all journeys which depart at 'from' at the given departure time.
        /// 
        ///  Performs an Earliest Arrival Scan till as long as 'lastArrival' is not passed.
        /// 
        ///  the Journey will contain a 'Choice'-element
        ///  
        ///  </summary>
        public static IReadOnlyDictionary<(uint localTileId, uint localId), Journey<T>> Isochrone<T>
        (this TransitDb.TransitDbSnapShot snapshot,
            Profile<T> profile,
            string from, DateTime departure, DateTime lastArrival)
            where T : IJourneyStats<T>
        {
            var fromId = snapshot.FindStop(from, $"Departure location {from} was not found");

            /*
             * We construct an Earliest Connection Scan.
             * A bit peculiar: there is _no_ arrival station specified.
             * This will cause EAS to scan all connections until 'lastArrival' has been reached;
             * to conclude that 'no journey to any of the specified arrival stations was found'.
             *
             * EAS.calculateJourneys will thus be null - but meanwhile every reachable station will be marked.
             * And it is exactly that which we need!
             */
            var eas = new EarliestConnectionScan<T>(snapshot,
                new List<(uint localTileId, uint localId)> {fromId},
                new List<(uint localTileId, uint localId)>(), // EMPTY LIST!
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
        ///
        /// </summary>
        /// <returns></returns>
        public static Journey<T> LatestDeparture<T>
        (this TransitDb.TransitDbSnapShot snapshot,
            Profile<T> profile, string from, string to, DateTime departure, DateTime lastArrival)
            where T : IJourneyStats<T>
        {
            var reader = snapshot.StopsDb.GetReader();
            var fromId = reader.FindStop(from);
            var toId = reader.FindStop(to);

            if (fromId.Equals(toId))
            {
                throw new ArgumentException($"The departure and arrival arguments are the same ({from})");
            }

            var las = new LatestConnectionScan<T>(
                snapshot,
                new List<(uint localTileId, uint localId)> {fromId},
                new List<(uint localTileId, uint localId)> {toId},
                departure.ToUnixTime(), lastArrival.ToUnixTime(),
                profile
            );
            return las.CalculateJourney();
        }

        /// <summary>
        /// Calculates all journeys which arrive at 'to' at last at the given arrival time.
        ///
        /// Performs a Latest Arrival Scan till as long as 'lastArrival' is not passed.
        ///
        /// </summary>
        /// <returns></returns>
        public static Dictionary<(uint localTileId, uint localId), Journey<T>> IsochroneLatestArrival<T>
        (this TransitDb.TransitDbSnapShot snapshot,
            Profile<T> profile, string to, DateTime departure, DateTime lastArrival)
            where T : IJourneyStats<T>
        {
            var reader = snapshot.StopsDb.GetReader();
            if (!reader.MoveTo(to)) throw new ArgumentException($"Departure location {to} was not found");
            var toId = reader.Id;


            /*
             * Same principle as the other IsochroneFunction
             */
            var las = new LatestConnectionScan<T>(
                snapshot,
                new List<(uint localTileId, uint localId)>(), // EMPTY LIST!
                new List<(uint localTileId, uint localId)> {toId},
                departure.ToUnixTime(), lastArrival.ToUnixTime(),
                profile
            );
            las.CalculateJourney();

            var allJourneys = las.GetAllJourneys();


            var reversedJourneys = new Dictionary<(uint localTileId, uint localId), Journey<T>>();
            foreach (var pair in allJourneys)
            {
                // Due to the nature of LAS, there can be no choices in the journeys; reversal will only return one value
                var prototype = pair.Value.Reversed()[0];
                reversedJourneys.Add(pair.Key, prototype);
            }

            return reversedJourneys;
        }

        /// <summary>
        ///
        /// Calculates the profiles Journeys for the given coordinates.
        /// 
        /// Starts with an EAS, then gives profiled journeys.
        /// 
        /// Note that the profile scan might scan in a window far smaller then the last-arrival time
        ///
        /// If either departureTime of arrivalTime are given (but _not_ both), the earliestArrivalScan will be run.
        /// The time of this EAS-journey (*2) is used as lookahead.
        /// However, running this EAS needs a bound too. This bound is given by lookAhead (time in seconds) and will not be crossed
        /// 
        /// </summary>
        public static List<Journey<T>> CalculateJourneys<T>
        (this TransitDb.TransitDbSnapShot snapshot,
            Profile<T> profile, string from, string to,
            DateTime? departure = null, DateTime? arrival = null, uint lookAhead = 24 * 60 * 60)
            where T : IJourneyStats<T>
        {
            var reader = snapshot.StopsDb.GetReader();
            if (!reader.MoveTo(from)) throw new ArgumentException($"Departure location {from} was not found");
            var fromId = reader.Id;
            if (!reader.MoveTo(to)) throw new ArgumentException($"Arrival location {to} was not found");
            var toId = reader.Id;
            return snapshot.CalculateJourneys(profile,
                fromId, toId,
                departure?.ToUnixTime() ?? 0,
                arrival?.ToUnixTime() ?? 0,
                lookAhead);
        }

        ///  <summary>
        ///
        /// Calculates the profiles Journeys for the given coordinates.
        /// 
        /// Starts with an EAS, then gives profiled journeys.
        /// 
        /// Note that the profile scan might scan in a window far smaller then the last-arrivaltime
        ///
        /// If either departureTime of arrivalTime are given (but _not_ both), the earliestArrivalScan will be run.
        /// The time of this EAS-journey (*2) is used as lookahead.
        /// However, running this EAS needs a bound too. This bound is given by lookAhead (time in seconds) and will not be crossed
        /// 
        ///  </summary>
        public static List<Journey<T>> CalculateJourneys<T>
        (this TransitDb.TransitDbSnapShot snapshot,
            Profile<T> profile, (uint, uint) depLocation, (uint, uint) arrivalLocation,
            ulong departureTime = 0, ulong lastArrivalTime = 0, uint lookAhead = 24 * 60 * 60)
            where T : IJourneyStats<T>
        {
            if (departureTime == 0 && lastArrivalTime == 0)
            {
                throw new ArgumentException("At least one of departure or arrival time should be given");
            }

            if (depLocation == arrivalLocation)
            {
                throw new ArgumentException("Departure and arrival location are the same");
            }

            if (departureTime == 0)
            {
                var las = new LatestConnectionScan<T>(snapshot, depLocation, arrivalLocation,
                    lastArrivalTime - lookAhead, lastArrivalTime,
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
            }

            var lastArrivalTimeSet = lastArrivalTime != 0;
            if (!lastArrivalTimeSet)
            {
                lastArrivalTime = departureTime + lookAhead;
            }

            var eas = new EarliestConnectionScan<T>(snapshot,
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
                return null;
            }

            departureTime = earliestJourney.Root.DepartureTime();
            if (!lastArrivalTimeSet)
            {
                lastArrivalTime = earliestJourney.ArrivalTime() +
                                  (earliestJourney.ArrivalTime() - earliestJourney.Root.DepartureTime());
            }

            IConnectionFilter filter = eas;

            var pcs = new ProfiledConnectionScan<T>(
                snapshot,
                depLocation, arrivalLocation,
                departureTime, lastArrivalTime,
                profile,
                filter
            );

            return pcs.CalculateJourneys();
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
        public static List<Journey<T>> CalculateJourneys<T>(this TransitDb.TransitDbSnapShot snapshot,
            Profile<T> profile, 
            (uint tileId, uint localId) departureStop, (uint tileId, uint localId) arrivalStop,
            DateTime? departure = null, 
            DateTime? arrival = null, 
            uint lookAheadInSeconds = 24 * 60 * 60)
            where T : IJourneyStats<T>
        {
            return snapshot.CalculateJourneys(profile, departureStop, arrivalStop,
                departure?.ToUnixTime() ?? 0, arrival?.ToUnixTime() ?? 0, lookAheadInSeconds);
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
        public static List<Journey<T>> CalculateJourneys<T>(this TransitDb.TransitDbSnapShot snapshot,
            Profile<T> profile, 
            IStop departureStop, IStop arrivalStop,
            DateTime? departure = null, 
            DateTime? arrival = null, 
            uint lookAheadInSeconds = 24 * 60 * 60)
            where T : IJourneyStats<T>
        {
            return snapshot.CalculateJourneys(profile, departureStop.Id, arrivalStop.Id,
                departure?.ToUnixTime() ?? 0, arrival?.ToUnixTime() ?? 0, lookAheadInSeconds);
        }
    }
}