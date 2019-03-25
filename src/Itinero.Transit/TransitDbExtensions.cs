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
            return snapShot.StopsDb.GetReader().SearchClosest(longitude, latitude, maxDistanceInMeters);
        }

        public static LocationId FindStop(this TransitDb.TransitDbSnapShot snapshot, string locationId,
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

            var settings = new ScanSettings<T>(
                snapshot, departure, lastArrival, profile.StatsFactory,
                profile.ProfileComparator, profile.InternalTransferGenerator, profile.WalksGenerator, fromId, toId
            );

            var eas = new EarliestConnectionScan<T>(settings);
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
        public static IReadOnlyDictionary<LocationId, Journey<T>> Isochrone<T>
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
            var settings = new ScanSettings<T>(
                snapshot, departure, lastArrival, profile.StatsFactory,
                profile.ProfileComparator, profile.InternalTransferGenerator, profile.WalksGenerator,
                new List<LocationId> {fromId},
                new List<LocationId>() // EMPTY LIST
            );
            var eas = new EarliestConnectionScan<T>(settings);
            eas.CalculateJourney();

            return eas.Isochrone();
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

            var settings = new ScanSettings<T>(
                snapshot, departure, lastArrival, profile.StatsFactory,
                profile.ProfileComparator, profile.InternalTransferGenerator, profile.WalksGenerator, fromId, toId
            );
            var las = new LatestConnectionScan<T>(settings);
            return las.CalculateJourney();
        }

        /// <summary>
        /// Calculates all journeys which arrive at 'to' at last at the given arrival time.
        ///
        /// Performs a Latest Arrival Scan till as long as 'lastArrival' is not passed.
        ///
        /// </summary>
        /// <returns></returns>
        public static Dictionary<LocationId, Journey<T>> IsochroneLatestArrival<T>
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
            var settings = new ScanSettings<T>(
                snapshot, departure, lastArrival, profile.StatsFactory,
                profile.ProfileComparator, profile.InternalTransferGenerator, profile.WalksGenerator,
                new List<LocationId>(), // EMPTY LIST
                new List<LocationId> {toId}
            );
            var las = new LatestConnectionScan<T>(settings);
            las.CalculateJourney();

            var allJourneys = las.Isochrone();


            var reversedJourneys = new Dictionary<LocationId, Journey<T>>();
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
            Profile<T> profile,
            string from, string to,
            DateTime? departure = null, DateTime? arrival = null)
            where T : IJourneyStats<T>
        {
            var reader = snapshot.StopsDb.GetReader();
            if (!reader.MoveTo(from)) throw new ArgumentException($"Departure location {from} was not found");
            var fromId = reader.Id;
            if (!reader.MoveTo(to)) throw new ArgumentException($"Arrival location {to} was not found");
            var toId = reader.Id;
            return snapshot.CalculateJourneys(profile,
                fromId, toId,
                departure,
                arrival);
        }

        private static List<Journey<T>> CalculateJourneys<T>(ScanSettings<T> settings, Profile<T> profile)
            where T : IJourneyStats<T>
        {
            settings.SanityCheck();

            if (settings.EarliestDeparture == DateTime.MinValue)
            {
                // No start time given: we calculate one with LAS
                var las = new LatestConnectionScan<T>(settings);
                var lasJourney = las.CalculateJourney(
                    (journeyArr, journeyDep) =>
                    {
                        var diff = journeyArr - journeyDep;
                        return journeyArr - (ulong) profile.LookAhead(TimeSpan.FromSeconds(diff)).TotalSeconds;
                    });
                var lasTimeSpan = lasJourney.ArrivalTime() - lasJourney.Root.DepartureTime();
                settings.LastArrival = lasJourney.ArrivalTime().FromUnixTime();
                settings.EarliestDeparture =
                    settings.LastArrival - profile.LookAhead(TimeSpan.FromSeconds(lasTimeSpan));


                // We run an Eas too, to give optimization data to PCS later on
                var eas = new EarliestConnectionScan<T>(settings);
                var easJourney = eas.CalculateJourney(
                    (dep, arr) => settings.LastArrival.ToUnixTime());

                settings.EarliestDeparture = easJourney.Root.DepartureTime().FromUnixTime();

                settings.ExampleJourney = easJourney;
                settings.Filter = eas.AsFilter();
            }
            else if (settings.LastArrival == DateTime.MinValue)
            {
                // No end time given: we calculate on with EAS
                var eas = new EarliestConnectionScan<T>(settings);
                var easJourney = eas.CalculateJourney(
                    (journeyArr, journeyDep) =>
                    {
                        var diff = journeyArr - journeyDep;
                        return journeyArr + (ulong) profile.LookAhead(TimeSpan.FromSeconds(diff)).TotalSeconds;
                    });
                var easTimeSpan = easJourney.ArrivalTime() - easJourney.Root.DepartureTime();
                settings.EarliestDeparture = easJourney.Root.DepartureTime().FromUnixTime();
                settings.LastArrival =
                    settings.EarliestDeparture + profile.LookAhead(TimeSpan.FromSeconds(easTimeSpan));
                settings.ExampleJourney = easJourney;
                settings.Filter = eas.AsFilter();
            }
            else
            {
                // Both start and end times are given
                // We still run an EAS to optimize PCS afterwards
                var eas = new EarliestConnectionScan<T>(settings);
                var easJourney = eas.CalculateJourney(
                    ((dep, arr) => settings.LastArrival.ToUnixTime()));

                settings.EarliestDeparture = easJourney.Root.DepartureTime().FromUnixTime();

                settings.ExampleJourney = easJourney;
                settings.Filter = eas.AsFilter();
            }

            if (settings.ExampleJourney == null)
            {
                // EAS earlier on didn't find anything
                return null;
            }

            var pcs = new ProfiledConnectionScan<T>(settings);

            return pcs.CalculateJourneys();
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
            Profile<T> profile, LocationId depLocation, LocationId arrivalLocation,
            DateTime? departureTime = null, DateTime? lastArrivalTime = null) where T : IJourneyStats<T>
        {
            var settings = new ScanSettings<T>(
                snapshot, 
                departureTime ?? DateTime.MinValue, lastArrivalTime ?? DateTime.MinValue, 
                profile.StatsFactory,profile.ProfileComparator,
                profile.InternalTransferGenerator, profile.WalksGenerator, 
                depLocation, arrivalLocation
            );
            return CalculateJourneys(settings, profile);
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
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<Journey<T>> CalculateJourneys<T>(this TransitDb.TransitDbSnapShot snapshot,
            Profile<T> profile,
            IStop departureStop, IStop arrivalStop,
            DateTime? departure = null,
            DateTime? arrival = null)
            where T : IJourneyStats<T>
        {
            return snapshot.CalculateJourneys(profile, departureStop.Id, arrivalStop.Id,
                departure, arrival);
        }

        /// <summary>
        /// When running PCS with CalculateJourneys, sometimes 'families' of journeys pop up.
        ///
        /// Such a family consists of a set of journeys, where each journey has
        /// - The same departure time
        /// - The same arrival time
        /// - The same number of transfers.
        ///
        /// In other words, they are identical in terms of the already applied statistic T on a 'profileComparison'-basis.
        ///
        /// Of course, ppl don't like too much options; this method applies another statistic on a journey and filters based on this second statistic.
        ///
        /// The list of journeys must be ordered by departure time
        /// 
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static List<Journey<T>> PruneInAlternatives<S, T>(
            this IEnumerable<Journey<T>> profiledJourneys,
            S newStatistic,
            StatsComparator<S> newComparer)
            where T : IJourneyStats<T>
            where S : IJourneyStats<S>
        {
            var result = new List<Journey<T>>();

            var alreadySeen = new HashSet<Journey<T>>();

            Journey<T> lastT = null;
            Journey<S> lastS = null;

            foreach (var j in profiledJourneys)
            {
                if (alreadySeen.Contains(j))
                {
                    // This exact journey is a duplicate
                    continue;
                }

                alreadySeen.Add(j);

                Journey<S> jS = null;
                if (lastT != null && lastT.Time == j.Time && lastT.Root.Time == j.Root.Time)
                {
                    // The previous and current journeys have the same departure and arrival time
                    // We assume they are part of a family and have the same number of transfers
                    // So, we apply the statistic S...
                    lastS = lastS ?? lastT.MeasureWith(newStatistic);
                    jS = j.MeasureWith(newStatistic);
                    // ... and we compare

                    var comparison = newComparer.ADominatesB(lastS, jS);
                    if (comparison < 0)
                    {
                        // lastS is better
                        // We ignore the current element
                        continue;
                    }

                    if (comparison > 0)
                    {
                        // Current one is better
                        // We overwrite the previous element
                        result[result.Count - 1] = j;
                        lastT = j;
                        lastS = jS;
                        continue;
                    }

                    // Else:
                    // Both options bring something to the table and should be kept
                    // We just let the loop run its course
                }

                // either the departure and arrival time are different here
                // Or the big if statement above fell through till here
                result.Add(j);
                lastT = j;
                lastS = jS;
            }


            return result;
        }
    }
}