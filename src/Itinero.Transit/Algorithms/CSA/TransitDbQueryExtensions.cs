using System;
using System.Collections.Generic;
using Itinero.IO.LC;
using Itinero.Transit.Data;
using Itinero.Transit.Journeys;
using Itinero.Transit.Logging;

namespace Itinero.Transit.Algorithms.CSA
{
    /// <summary>
    /// This class is the main entry point to request routes as library user.
    /// It exposes all the core algorithms in a consistent and easy to use way.
    /// </summary>
    public static class TransitDbQueryExtensions
    {
        public static (uint, uint) FindStop(this TransitDb tdb, string locationId, string errMsg = null)
        {
            return tdb.Latest.StopsDb.GetReader().FindStop(locationId, errMsg);
        }


        ///  <summary>
        ///  Calculates the earliest arriving journey which depart at 'from' at the given departure time and arrives at 'to'.
        /// 
        ///  Performs an Earliest Arrival Scan
        ///
        /// </summary>
        /// <param name="tdb">The transit DB containing the PT-data</param>
        /// <param name="profile"></param>
        ///  <param name="from"></param>
        ///  <param name="departure"></param>
        ///  <param name="lastArrival"></param>
        ///  <typeparam name="T"></typeparam>
        ///  <returns></returns>
        public static Journey<T> EarliestArrival<T>
        (this TransitDb tdb,
            Profile<T> profile,
            string from, string to,
            DateTime departure, DateTime lastArrival)
            where T : IJourneyStats<T>
        {
            tdb.UpdateTimeFrame(departure, lastArrival);

            var reader = tdb.Latest.StopsDb.GetReader();
            var fromId = reader.FindStop(from, $"Departure location {from} was not found");
            var toId = reader.FindStop(to, $"Departure location {to} was not found");

            if (fromId.Equals(toId))
            {
                throw new ArgumentException($"The departure and arrival arguments are the same ({from})");
            }

            var eas = new EarliestConnectionScan<T>(tdb,
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
        (this TransitDb tdb,
            Profile<T> profile,
            string from, DateTime departure, DateTime lastArrival)
            where T : IJourneyStats<T>
        {
            tdb.UpdateTimeFrame(departure, lastArrival);

            var fromId = tdb.FindStop(from, $"Departure location {from} was not found");

            /*
             * We construct an Earliest Connection Scan.
             * A bit peculiar: there is _no_ arrival station specified.
             * This will cause EAS to scan all connections until 'lastArrival' has been reached;
             * to conclude that 'no journey to any of the specified arrival stations was found'.
             *
             * EAS.calculateJourneys will thus be null - but meanwhile every reachable station will be marked.
             * And it is exactly that which we need!
             */
            var eas = new EarliestConnectionScan<T>(tdb,
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
        (this TransitDb tdb,
            Profile<T> profile, string from, string to, DateTime departure, DateTime lastArrival)
            where T : IJourneyStats<T>
        {
            tdb.UpdateTimeFrame(departure, lastArrival);

            var reader = tdb.Latest.StopsDb.GetReader();
            var fromId = reader.FindStop(from);
            var toId = reader.FindStop(to);

            if (fromId.Equals(toId))
            {
                throw new ArgumentException($"The departure and arrival arguments are the same ({from})");
            }

            var las = new LatestConnectionScan<T>(
                tdb,
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
        (this TransitDb tdb,
            Profile<T> profile, string to, DateTime departure, DateTime lastArrival)
            where T : IJourneyStats<T>
        {
            tdb.UpdateTimeFrame(departure, lastArrival);

            var reader = tdb.Latest.StopsDb.GetReader();
            if (!reader.MoveTo(to)) throw new ArgumentException($"Departure location {to} was not found");
            var toId = reader.Id;


            /*
             * Same principle as the other IsochroneFunction
             */
            var las = new LatestConnectionScan<T>(
                tdb,
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
        ///  Starts with an EAS, then gives profiled journeys.
        /// 
        ///  Note that the profile scan might scan in a window far smaller then the last-arrival time
        /// 
        /// </summary>
        public static IEnumerable<Journey<T>> CalculateJourneys<T>
        (this TransitDb tdb,
            Profile<T> profile, string from, string to,
            DateTime? departure = null, DateTime? arrival = null) where T : IJourneyStats<T>
        {
            var reader = tdb.Latest.StopsDb.GetReader();
            if (!reader.MoveTo(from)) throw new ArgumentException($"Departure location {from} was not found");
            var fromId = reader.Id;
            if (!reader.MoveTo(to)) throw new ArgumentException($"Arrival location {to} was not found");
            var toId = reader.Id;
            return tdb.CalculateJourneys(profile,
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
        public static IEnumerable<Journey<T>> CalculateJourneys<T>
        (this TransitDb tdb,
            Profile<T> profile, (uint, uint) depLocation, (uint, uint) arrivalLocation,
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
                tdb.UpdateTimeFrame((lastArrivalTime - 24 * 60 * 60).FromUnixTime(),
                    lastArrivalTime.FromUnixTime());
                var las = new LatestConnectionScan<T>(tdb, depLocation, arrivalLocation,
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
            tdb.UpdateTimeFrame(departureTime.FromUnixTime(), lastArrivalTime.FromUnixTime());
            var eas = new EarliestConnectionScan<T>(tdb,
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
                tdb,
                depLocation, arrivalLocation,
                departureTime, lastArrivalTime,
                profile,
                filter
            );

            return pcs.CalculateJourneys();
        }
    }
}