using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Algorithms.Filter;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey;
using Itinero.Transit.Logging;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Utils;

// ReSharper disable PossibleMultipleEnumeration

// ReSharper disable UnusedMember.Global

// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit
{
    /// <summary>
    /// This class is the main entry point to request routes as library user.
    /// It exposes all the core algorithms in a consistent and easy to use way.
    /// </summary>
    public static class TransitDbExtensions
    {
        public static WithProfile<T> SelectProfile<T>(this IEnumerable<TransitDbSnapShot> tdbs,
            Profile<T> profile)
            where T : IJourneyMetric<T>
        {
            return new WithProfile<T>(tdbs, profile);
        }

        public static WithProfile<T> SelectProfile<T>(this IEnumerable<TransitDb> tdbs, Profile<T> profile)
            where T : IJourneyMetric<T>
        {
            return tdbs.Select(tdb => tdb.Latest).SelectProfile(profile);
        }


        public static WithProfile<T> SelectProfile<T>(this TransitDb tdb, Profile<T> profile)
            where T : IJourneyMetric<T>
        {
            return tdb.Latest.SelectProfile(profile);
        }

        public static WithProfile<T> SelectProfile<T>(this TransitDbSnapShot tdb, Profile<T> profile)
            where T : IJourneyMetric<T>
        {
            return SelectProfile(new List<TransitDbSnapShot> {tdb}, profile);
        }

        /// <summary>
        /// Finds the closest stop near 'around.
        /// </summary>
        /// <param name="snapShot">The snapshot.</param>
        /// <param name="around">Search around this stop</param>
        /// <param name="maxDistanceInMeters">The maximum distance in meters.</param>
        /// <returns>The closest stop.</returns>
        public static Stop FindClosestStop(this TransitDbSnapShot snapShot, Stop around,
            uint maxDistanceInMeters = 1000)
        {
            return snapShot.StopsDb.FindClosest(around, maxDistanceInMeters);
        }

        /// <summary>
        /// Finds the closest stop.
        /// </summary>
        /// <param name="snapShot">The snapshot.</param>
        /// <param name="around">The stop to search around.</param>
        /// <param name="maxDistanceInMeters">The maximum distance in meters.</param>
        /// <returns>The closest stop.</returns>
        public static Stop FindClosestStop(this IEnumerable<TransitDbSnapShot> snapShot, Stop around,
            uint maxDistanceInMeters = 1000)
        {
            var reader = StopsDbAggregator.CreateFrom(snapShot);
            return reader.FindClosest(around, maxDistanceInMeters);
        }

        public static Stop FindStop(this TransitDbSnapShot snapshot, string locationId,
            string errMsg = null)
        {
            return snapshot.StopsDb.Get(locationId, errMsg);
        }

        public static Stop FindStop(this IEnumerable<TransitDbSnapShot> snapshots, string locationId,
            string errMsg = null)
        {
            return StopsDbAggregator.CreateFrom(snapshots)
                .Get(locationId, errMsg);
        }

        public static IEnumerable<Stop> FindStops(this IEnumerable<TransitDbSnapShot> snapshots,
            IEnumerable<string> locationIds,
            Func<string, string> errMsg = null)
        {
            var reader = StopsDbAggregator.CreateFrom(snapshots);
            foreach (var id in locationIds)
            {
                yield return reader.Get(id, errMsg?.Invoke(id));
            }
        }

        public static IEnumerable<Stop> FindStops(this IStopsDb reader,
            IEnumerable<string> locationIds,
            Func<string, string> errMsg = null)
        {
            foreach (var id in locationIds)
            {
                yield return reader.Get(id, errMsg?.Invoke(id));
            }
        }


        public static Stop FindStops(this IStopsDb stops,
            string id,
            Func<string, string> errMsg = null)
        {
            return stops.Get(id, errMsg?.Invoke(id));
        }


        /// <summary>
        /// Given a list of profiled journeys (sorted by departure time),
        /// this method sifts through them and searches for families*
        ///
        /// From every family, one winning member is kept. The winner is selected based on the comparator,
        /// which will attempt to select the _minimal_ journey
        ///
        ///
        /// * a 'family' is a set of journeys which:
        /// - Have identical departure and arrival times
        /// - Have identical departure and arrival locations
        /// - Have an identical score on the metric
        /// </summary>
        /// <returns></returns>
        public static List<Journey<T>> PruneFamilies<T>(
            this List<Journey<T>> journeys,
            IComparer<Journey<T>> comparer) where T : IJourneyMetric<T>
        {
            if (journeys.Count == 0)
            {
                return journeys;
            }

            var result = new List<Journey<T>>();

            var currentTime = journeys[0].Root.Time;
            var currentList = new List<Journey<T>>();

            foreach (var j in journeys)
            {
                if (j.Root.Time != currentTime)
                {
                    result.AddRange(currentList);
                    currentList = new List<Journey<T>>();
                    currentTime = j.Root.Time;
                }


                // In what family does this journey fit?
                var foundFamilyMember = false;
                for (var i = 0; i < currentList.Count; i++)
                {
                    var representative = currentList[i];
                    if (
                        representative.Root.DepartureTime() == j.Root.DepartureTime() &&
                        representative.ArrivalTime() == j.ArrivalTime() &&
                        representative.Root.Location.Equals(j.Root.Location) &&
                        representative.Location.Equals(j.Location) &&
                        representative.Metric.Equals(j.Metric))
                    {
                        foundFamilyMember = true;

                        // We found a journey with the same properties
                        // This town is not big enough for both of them
                        // So we have a shootout!
                        if (comparer.Compare(representative, j) < 0)
                        {
                            // Representative is smaller then j, so it is the winner
                            currentList[i] = j;
                        }

                        break;
                    }
                }

                if (!foundFamilyMember)
                {
                    currentList.Add(j);
                }
            }

            result.AddRange(currentList);

            return result;
        }

        public static Dictionary<ulong, List<HashSet<Journey<T>>>> PartitionFamilies<T>(
            this List<Journey<T>> journeys) where T : IJourneyMetric<T>
        {
            var result = new Dictionary<ulong, List<HashSet<Journey<T>>>>();

            if (journeys.Count == 0)
            {
                return result;
            }

            var currentTime = journeys[0].Root.Time;
            var currentList = new List<HashSet<Journey<T>>>();
            result[currentTime] = currentList;

            foreach (var j in journeys)
            {
                if (j.Root.Time != currentTime)
                {
                    currentList = new List<HashSet<Journey<T>>>();
                    currentTime = j.Root.Time;
                    result[currentTime] = currentList;
                }


                // In what family does this journey fit?
                HashSet<Journey<T>> foundFamily = null;
                foreach (var family in currentList)
                {
                    var representative = family.First();
                    if (
                        representative.Root.DepartureTime() == j.Root.DepartureTime() &&
                        representative.ArrivalTime() == j.ArrivalTime() &&
                        representative.Root.Location.Equals(j.Root.Location) &&
                        representative.Location.Equals(j.Location) &&
                        representative.Metric.Equals(j.Metric))
                    {
                        foundFamily = family;
                        break;
                    }
                }

                if (foundFamily == null)
                {
                    foundFamily = new HashSet<Journey<T>>();
                    currentList.Add(foundFamily);
                }

                foundFamily.Add(j);
            }

            return result;
        }
    }


    public class WithProfile<T> where T : IJourneyMetric<T>
    {
        public readonly IStopsDb StopsDb;
        public readonly IConnectionsDb ConnectionsDb;
        public readonly Profile<T> Profile;

        internal WithProfile(
            IStopsDb stops,
            IConnectionsDb connections,
            Profile<T> profile)
        {
            StopsDb = stops;
            ConnectionsDb = connections;
            Profile = profile;
        }

        internal WithProfile(IEnumerable<TransitDbSnapShot> tdbs, Profile<T> profile)
        {
            StopsDb = StopsDbAggregator.CreateFrom(tdbs);
            ConnectionsDb =
                ConnectionsDbAggregator.CreateFrom(tdbs.Select(tdb => tdb.ConnectionsDb).ToList());
            Profile = new Profile<T>(
                profile.InternalTransferGenerator,
                profile.WalksGenerator,
                profile.MetricFactory,
                profile.ProfileComparator,
                profile.ConnectionFilter,
                profile.JourneyFilter
            );


            var alreadyUsedIds = new HashSet<uint>();
            foreach (var tdb in tdbs)
            {
                if (alreadyUsedIds.Contains(tdb.Id))
                {
                    throw new ArgumentException("Duplicate identifiers");
                }

                alreadyUsedIds.Add(tdb.Id);
            }
        }


        /// <summary>
        /// Runs the 'closest stops' search as specified by the profile for every stop in the dataset.
        /// Might speed up actual calculations.
        ///
        /// This method is run synchronously.
        /// This returns a *new* WithProfile, where the stopsReader uses a cache.
        /// If memory is tight and only a few queries will be ran, don't use this.
        /// </summary>
        [Pure]
        public WithProfile<T> PrecalculateClosestStops()
        {
            Log.Information("Caching reachable locations");
            var start = DateTime.Now;

            var withCache = StopsDb.UseCache();
            var walksGenCache = Profile.WalksGenerator.UseCache();
            walksGenCache.PreCalculateCache(withCache);

            var end = DateTime.Now;
            Log.Information($"Caching reachable locations took {(end - start).TotalMilliseconds}ms");
            return new WithProfile<T>(
                withCache,
                ConnectionsDb,
                new Profile<T>(
                    Profile.InternalTransferGenerator,
                    walksGenCache,
                    Profile.MetricFactory,
                    Profile.ProfileComparator
                )
            );
        }

        /// <summary>
        /// This method is mainly used to inject a floating StopsReader into the profile.
        ///
        /// Do think about the caching behaviour:
        /// tdbs.UseProfile(p).PrecalculateClosestStops().AddStopsReader([some floating which accumulates stops])
        /// will not cache the floating points.
        /// </summary>
        [Pure]
        public WithProfile<T> AddStopsReader(IStopsDb stopsReader)
        {
            return new WithProfile<T>(
                StopsDbAggregator.CreateFrom(new List<IStopsDb>
                {
                    stopsReader, StopsDb
                }),
                ConnectionsDb,
                Profile
            );
        }

        [Pure]
        public WithProfile<T> SetStopsDb(IStopsDb stopsReader)
        {
            return new WithProfile<T>(
                stopsReader,
                ConnectionsDb,
                Profile
            );
        }


        /// <summary>
        /// In some cases the traveller has multiple options to depart or arrive,
        /// and only wants to know the fastest route between any two of them.
        ///
        /// This constructor allows to perform such queries.
        ///
        /// E.g. Departures are {A, B, C}, arrivals are {X, Y, Z}.
        /// The earliest arrival scan at a certain time could be for example some journey from A to Y,
        /// whereas the 'AllJourneys' (profiled search) could give one option C to X and another option B to Y, ignoring A and Z altogether.
        /// 
        /// </summary>
        public IWithSingleLocation<T> SelectSingleStop(IEnumerable<Stop> stop)
        {
            return new WithLocation<T>(StopsDb, ConnectionsDb, Profile, stop, stop);
        }
        
        /// <summary>
        /// In some cases the traveller has multiple options to depart or arrive,
        /// and only wants to know the fastest route between any two of them.
        ///
        /// This constructor allows to perform such queries.
        ///
        /// E.g. Departures are {A, B, C}, arrivals are {X, Y, Z}.
        /// The earliest arrival scan at a certain time could be for example some journey from A to Y,
        /// whereas the 'AllJourneys' (profiled search) could give one option C to X and another option B to Y, ignoring A and Z altogether.
        /// 
        /// </summary>
        public IWithSingleLocation<T> SelectSingleStop(IEnumerable<StopId> stop)
        {
            var stops = StopsDb.GetAll(stop.ToList());
            return SelectSingleStop(stops);
        }

        /// <summary>
        /// In some cases the traveller has multiple options to depart or arrive,
        /// and only wants to know the fastest route between any two of them.
        ///
        /// This constructor allows to perform such queries.
        ///
        /// E.g. Departures are {A, B, C}, arrivals are {X, Y, Z}.
        /// The earliest arrival scan at a certain time could be for example some journey from A to Y,
        /// whereas the 'AllJourneys' (profiled search) could give one option C to X and another option B to Y, ignoring A and Z altogether.
        /// 
        /// </summary>
        public IWithSingleLocation<T> SelectSingleStop(IEnumerable<string> stop)
        {
            return SelectSingleStop(
                StopsDb.FindStops(stop, f => $"Stop {f} was not found")
            );
        }


        public IWithSingleLocation<T> SelectSingleStop(StopId stop)
        {
            return SelectSingleStop(new List<Stop> {StopsDb.Get(stop)});
        }

        public IWithSingleLocation<T> SelectSingleStop(Stop stop)
        {
            return SelectSingleStop(stop.GlobalId);
        }

        public IWithSingleLocation<T> SelectSingleStop(string stop)
        {
            if (!StopsDb.TryGetId(stop, out var id))
            {
                throw new ArgumentException($"Stop {stop} was not found");
            }

            return SelectSingleStop(id);
        }


        public WithLocation<T> SelectStops(IEnumerable<StopId> from,
            IEnumerable<StopId> to)
        {
            return SelectStops(
                from.Select(id => StopsDb.Get(id)),
                to.Select(id => StopsDb.Get(id)));
        }

        public WithLocation<T> SelectStops(IEnumerable<string> from, IEnumerable<string> to)
        {
            return SelectStops(
                StopsDb.FindStops(from, f => $"Departure stop {f} was not found"),
                StopsDb.FindStops(to, t => $"Arrival stop {t} was not found")
            );
        }

        public WithLocation<T> SelectStops(IEnumerable<Stop> from, IEnumerable<Stop> to)
        {
            return new WithLocation<T>(StopsDb, ConnectionsDb, Profile, from,to);
        }

        public WithLocation<T> SelectStops(StopId from, StopId to)
        {
            return SelectStops(StopsDb.Get(from), StopsDb.Get(to));
        }

        public WithLocation<T> SelectStops(Stop from, Stop to)
        {
            return SelectStops(new []{from}, new []{to});
        }

        public WithLocation<T> SelectStops(string from, string to)
        {
            return SelectStops(
                StopsDb.Get(from, $"Departure stop {from} was not found"),
                StopsDb.Get(to, $"Arrival stop {to} was not found")
            );
        }
    }

    /// <summary>
    /// A 'WithSingleLocation' is a calculator object that has only a single location,
    /// which is used as either the departure or the arrival location.
    ///
    /// It can only be used to calculate isochrones
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IWithSingleLocation<T> where T : IJourneyMetric<T>
    {
        IWithTimeSingleLocation<T> SelectTimeFrame(
            DateTime start,
            DateTime end);

        IWithTimeSingleLocation<T> SelectTimeFrame(
            ulong start,
            ulong end);
    }

    public class WithLocation<T> :
        WithProfile<T>,
        IWithSingleLocation<T>
        where T : IJourneyMetric<T>
    {
        public readonly List<Stop> From, To;

        internal WithLocation(IStopsDb stopsReader,
            IConnectionsDb connections,
            Profile<T> profile,
            IEnumerable<Stop> from,
            IEnumerable<Stop> to) : base(stopsReader, connections, profile)
        {
            From = from.ToList();
            To = to.ToList();
        }


        public WithTime<T> SelectTimeFrame(
            DateTime start,
            DateTime end)
        {
            return new WithTime<T>(StopsDb, ConnectionsDb, Profile, From, To, start,
                end);
        }

        IWithTimeSingleLocation<T> IWithSingleLocation<T>.SelectTimeFrame(DateTime start, DateTime end)
        {
            return SelectTimeFrame(start, end);
        }

        public WithTime<T> SelectTimeFrame(
            ulong start,
            ulong end)
        {
            return SelectTimeFrame(start.FromUnixTime(), end.FromUnixTime());
        }


        IWithTimeSingleLocation<T> IWithSingleLocation<T>.SelectTimeFrame(ulong start, ulong end)
        {
            return SelectTimeFrame(start, end);
        }


        /// <summary>
        /// Calculates a walk between the given departure and arrival locations.
        /// This will not use public transport at all.
        /// </summary>
        /// <returns>A dictionary with timings. The key is a tuple of globalIds (fromGlobalId, toGlobalId), the value is how much seconds it takes. There will be no entry if that walk is not possible. If no walk is possible, the dictionary will be null.</returns>
        public Dictionary<(Stop from, Stop to), uint> CalculateDirectJourney()
        {
            var from = new List<Stop>();
            foreach (var fr in From)
            {
                from.Add(fr);
            }

            var to = new List<Stop>();

            foreach (var t in To)
            {
                to.Add(t);
            }

            return Profile.WalksGenerator.TimesBetween(from, to);
        }
    }

    public interface IWithTimeSingleLocation<T> where T : IJourneyMetric<T>
    {
        ///  <summary>
        ///  Calculates all journeys which depart at 'from' at the given departure time and arrive before the specified 'end'-time of the timeframe.
        ///  </summary>
        IReadOnlyDictionary<StopId, Journey<T>> CalculateIsochroneFrom();

        ///  <summary>
        ///  Calculates all journeys which arrive at 'to' at the given arrival time and departarter the specified 'start'-time of the timeframe.
        ///  </summary>
        IReadOnlyDictionary<StopId, Journey<T>> CalculateIsochroneTo();

        /// <summary>
        /// Calculates all journeys which are optimal for their given timeframe and which go to the destination stop.
        /// </summary>
        Dictionary<StopId, List<Journey<T>>> CalculateAllProfileJourneysTowards();
    }

    public class WithTime<T> :
        WithLocation<T>,
        IWithTimeSingleLocation<T>
        where T : IJourneyMetric<T>
    {
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }


        /// <summary>
        /// The filter constructed by other computations, e.g. by EAS
        /// </summary>
        internal IsochroneFilter<T> TimedFilter;

        internal WithTime(IStopsDb stopsReader,
            IConnectionsDb connectionEnumerator,
            Profile<T> profile,
            IEnumerable<Stop> from,
            IEnumerable<Stop> to,
            DateTime start,
            DateTime end) : base(stopsReader, connectionEnumerator, profile, from, to)
        {
            Start = start.ToUniversalTime();
            End = end.ToUniversalTime();

            if (Start > End)
            {
                throw new ArgumentException(
                    "Scan begin time (departure time) falls behind scan end time (arrival time)");
            }

            if (Start == End)
            {
                throw new ArgumentException(
                    "Scan begin time (departure time) is the same as scan end time (arrival time)");
            }

            if (Start == DateTime.MinValue)
            {
                throw new ArgumentException(
                    "Scan begin time (departure time) is DateTime.MinValue. Don't do this, this can cause (near)-infinite loops. Pick a sensible default instead (such as one day) in advance");
            }

            if (End == DateTime.MaxValue)
            {
                throw new ArgumentException(
                    "Scan end time (arrival time) is DateTime.MaxValue. Don't do this, this can cause (near)-infinite loops. Pick a sensible default instead (such as one day) after the start date");
            }
        }


        /// <summary>
        /// These are the scanSettings with all data.
        /// They can be used e.g. for EAS, LAS and PCS not for others.
        /// The main usage is testing though
        /// </summary>
        /// <returns></returns>
        internal ScanSettings<T> GetScanSettings()
        {
            return new ScanSettings<T>(
                StopsDb,
                ConnectionsDb,
                Start,
                End,
                Profile,
                From, To
            )
            {
                Filter = TimedFilter
            };
        }

        ///  <summary>
        ///  Calculates all journeys which depart at 'from' at the given departure time and arrive before the specified 'end'-time of the timeframe.
        /// This ignores the given 'to'-location
        ///  </summary>
        public IReadOnlyDictionary<StopId, Journey<T>> CalculateIsochroneFrom()
        {
            CheckHasFrom();
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
                StopsDb,
                ConnectionsDb,
                Start, End,
                Profile,
                From,
                new List<Stop>() // EMPTY LIST
            );
            var eas = new EarliestConnectionScan<T>(settings);
            eas.CalculateJourney();
            UseFilter(eas.AsFilter());
            return eas.Isochrone();
        }

        /// <inheritdoc />
        ///  <summary>
        ///  Calculates all journeys which arrive at 'to' at the given arrival time and departarter the specified 'start'-time of the timeframe.
        /// This ignores the given 'from'-location
        ///  </summary>
        public IReadOnlyDictionary<StopId, Journey<T>> CalculateIsochroneTo()
        {
            CheckHasTo();
            /*
             * Same principle as the other IsochroneFunction
             */
            var settings = new ScanSettings<T>(
                StopsDb,
                ConnectionsDb,
                Start,
                End,
                Profile,
                new List<Stop>(), // EMPTY LIST
                To
            );
            var las = new LatestConnectionScan<T>(settings);
            las.CalculateJourney();
            UseFilter(las.AsFilter());
            return las.Isochrone();
        }


        ///  <summary>
        ///  Calculates the journey which departs at 'from' and arrives at 'to' as early as possible.
        ///  </summary>
        /// <param name="expandSearch">
        /// EAS can be used to speed up PCS later on. However, PCS is often given a bigger time range to search.
        /// This function indicates a new timeframe-end to calculate PCS with later on.</param>
        /// <returns>A journey which is guaranteed to arrive as early as possible (or null if none was found)</returns>
        // ReSharper disable once UnusedMember.Global
        public Journey<T> CalculateEarliestArrivalJourney(
            Func<(DateTime journeyStart, DateTime journeyEnd), DateTime> expandSearch = null)
        {
            CheckAll();

            var settings = GetScanSettings();

            Func<ulong, ulong, ulong> expandSearchLong = null;

            if (expandSearch != null)
            {
                expandSearchLong = (start, end) =>
                    expandSearch((start.FromUnixTime(), end.FromUnixTime())).ToUnixTime();
            }

            var eas = new EarliestConnectionScan<T>(settings);
            var journey = eas.CalculateJourney(expandSearchLong);

            UseFilter(eas.AsFilter());
            if (journey != null && expandSearch != null)
            {
                End = expandSearch((journey.Root.Time.FromUnixTime(), journey.Time.FromUnixTime()));
            }

            return journey;
        }


        ///  <summary>
        ///  Calculates the journey which arrives at 'to' and departs at 'from' as late as possible.
        ///  </summary>
        /// <param name="expandSearch">
        /// LAS can be used to speed up PCS later on. However, PCS is often given a bigger time range to search.
        /// This function indicates a new timeframe-start to calculate PCS with later on.
        /// </param>
        /// <returns>A journey which is guaranteed to arrive as early as possible (or null if none was found)</returns>
        // ReSharper disable once UnusedMember.Global
        public Journey<T> CalculateLatestDepartureJourney(
            Func<(DateTime journeyStart, DateTime journeyEnd), DateTime> expandSearch = null)
        {
            CheckAll();
            var settings = GetScanSettings();

            Func<ulong, ulong, ulong> expandSearchLong = null;

            if (expandSearch != null)
            {
                expandSearchLong = (start, end) =>
                    expandSearch((start.FromUnixTime(), end.FromUnixTime())).ToUnixTime();
            }

            var las = new LatestConnectionScan<T>(settings);
            var journey = las.CalculateJourney(expandSearchLong);

            UseFilter(las.AsFilter());
            if (journey != null && expandSearch != null)
            {
                Start = expandSearch((journey.Root.Time.FromUnixTime(), journey.Time.FromUnixTime()));
            }

            return journey;
        }

        /// <summary>
        /// Calculates all journeys between departure and arrival stop during this timeframe.
        /// Every journey returned will have at least one public transport segment
        /// </summary>
        /// <remarks>
        /// Note that this list might contain families of very similar journeys, e.g. journeys which differ only in the transfer station taken.
        /// To prune them, use `PruneInAlternatives`
        /// </remarks>
        public List<Journey<T>> CalculateAllJourneys()
        {
            CheckAll();
            var settings = GetScanSettings();

            // TODO enable   settings.MetricGuesser =new SimpleMetricGuesser<T>(settings.ConnectionsEnumerator, settings.DepartureStop);

            var pcs = new ProfiledConnectionScan<T>(settings);
            return pcs.CalculateJourneys();
        }


        /// <summary>
        /// Calculates all journeys which are optimal for their given timeframe and which go to the destination stop.
        /// </summary>
        public Dictionary<StopId, List<Journey<T>>> CalculateAllProfileJourneysTowards()
        {
            CheckAll();
            var settings = new ScanSettings<T>(
                StopsDb,
                ConnectionsDb,
                Start,
                End,
                Profile,
                new List<Stop>(), // We don't pass any departure stop, as we want them all
                To
            )
            {
                Filter = TimedFilter
            };
            var pcs = new ProfiledConnectionScan<T>(settings);
            pcs.CalculateJourneys();
            return pcs.Isochrone();
        }

        /// <summary>
        /// Use the given filter.
        /// Note that some methods (Isochrone, EAS) might install a filter automatically too
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private void UseFilter(IsochroneFilter<T> filter)
        {
            TimedFilter = filter;
        }

        private void CheckNoOverlap()
        {
            if (From == null || From.Count == 0 || To == null || To.Count == 0)
            {
                return;
            }

            var overlap = From.FindAll(f => To.Contains(f));
            if (overlap.Any())
            {
                throw new Exception($"A departure stop is also used as arrival stop: {string.Join(", ", overlap)}");
            }
        }

        private void CheckHasFrom()
        {
            if (From == null || From.Count == 0)
            {
                throw new Exception("No departure stop(s) are given");
            }
        }

        private void CheckHasTo()
        {
            if (To == null || To.Count == 0)
            {
                throw new Exception("No arrival stop(s) are given");
            }
        }

        internal void CheckAll()
        {
            CheckHasFrom();
            CheckHasTo();
            CheckNoOverlap();
        }

        public void ResetFilter()
        {
            TimedFilter = null;
        }


        public override string ToString()
        {
            return $"withTime : {{  start: {Start:s}, end: {End:s}, from: [{string.Join(",", From)}]" +
                   $"to: [{string.Join(",", To)}], profile: {Profile}}}";
        }
    }
}