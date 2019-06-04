using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Journey;
using Itinero.Transit.Logging;
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
        public static WithProfile<T> SelectProfile<T>(this IEnumerable<TransitDb.TransitDbSnapShot> tdbs,
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

        public static WithProfile<T> SelectProfile<T>(this TransitDb.TransitDbSnapShot tdb, Profile<T> profile)
            where T : IJourneyMetric<T>
        {
            return SelectProfile(new List<TransitDb.TransitDbSnapShot> {tdb}, profile);
        }

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

        /// <summary>
        /// Finds the closest stop.
        /// </summary>
        /// <param name="snapShot">The snapshot.</param>
        /// <param name="longitude">The longitude.</param>
        /// <param name="latitude">The latitude.</param>
        /// <param name="maxDistanceInMeters">The maximum distance in meters.</param>
        /// <returns>The closest stop.</returns>
        public static IStop FindClosestStop(this IEnumerable<TransitDb.TransitDbSnapShot> snapShot, double longitude,
            double latitude,
            double maxDistanceInMeters = 1000)
        {
            return StopsReaderAggregator.CreateFrom(snapShot).SearchClosest(longitude, latitude, maxDistanceInMeters);
        }

        public static LocationId FindStop(this TransitDb.TransitDbSnapShot snapshot, string locationId,
            string errMsg = null)
        {
            return snapshot.StopsDb.GetReader().FindStop(locationId, errMsg);
        }

        public static LocationId FindStop(this IEnumerable<TransitDb.TransitDbSnapShot> snapshot, string locationId,
            string errMsg = null)
        {
            return StopsReaderAggregator.CreateFrom(snapshot).FindStop(locationId, errMsg);
        }

        public static IEnumerable<LocationId> FindStops(this IEnumerable<TransitDb.TransitDbSnapShot> snapshot,
            IEnumerable<string> locationIds,
            Func<string, string> errMsg = null)
        {
            var reader = StopsReaderAggregator.CreateFrom(snapshot);
            foreach (var id in locationIds)
            {
                yield return reader.FindStop(id, errMsg?.Invoke(id));
            }
        }

        public static IEnumerable<LocationId> FindStops(this IStopsReader reader,
            IEnumerable<string> locationIds,
            Func<string, string> errMsg = null)
        {
            foreach (var id in locationIds)
            {
                yield return reader.FindStop(id, errMsg?.Invoke(id));
            }
        }


        public static LocationId FindStops(this IStopsReader reader,
            string id,
            Func<string, string> errMsg = null)
        {
            return reader.FindStop(id, errMsg?.Invoke(id));
        }

        /// <summary>
        /// When running PCS with CalculateJourneys, sometimes 'families' of journeys pop up.
        ///
        /// Such a family consists of a set of journeys, where each journey has
        /// - The same departure time
        /// - The same arrival time
        /// - The same number of transfers.
        ///
        /// In other words, they are identical in terms of the already applied metrid T on a 'profileComparison'-basis.
        ///
        /// Of course, ppl don't like too much options; this method applies another metric on a journey and filters based on this second metric.
        ///
        /// The list of journeys must be ordered by departure time
        /// 
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static List<Journey<T>> PruneInAlternatives<S, T>(
            this IEnumerable<Journey<T>> profiledJourneys,
            S newMetric,
            MetricComparator<S> newComparer)
            where T : IJourneyMetric<T>
            where S : IJourneyMetric<S>
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

                if (lastT == null || lastT.Time != j.Time || lastT.Root.Time != j.Root.Time)
                {
                    result.Add(j);
                    lastT = j;
                    lastS = null;
                    continue;
                }

                // The previous and current journeys have the same departure and arrival time
                // We assume they are part of a family and have the same number of transfers
                // So, we apply the metric S...
                lastS = lastS ?? lastT.MeasureWith(newMetric);
                var jS = j.MeasureWith(newMetric);
                // ... and we compare

                var comparison = newComparer.ADominatesB(lastS, jS);
                if (comparison < 0)
                {
                    // lastS is better
                    // We ignore the current element
                    continue;
                }

                // Current one is better
                // We overwrite the previous element
                result[result.Count - 1] = j;
                lastT = j;
                lastS = jS;
            }


            return result;
        }
    }

    public class WithProfile<T> where T : IJourneyMetric<T>
    {
        internal readonly IStopsReader StopsReader;
        internal readonly IConnectionEnumerator ConnectionEnumerator;
        internal readonly IConnectionReader ConnectionReader;
        internal readonly Profile<T> Profile;

        internal WithProfile(IEnumerable<TransitDb.TransitDbSnapShot> tdbs, Profile<T> profile)
        {
            StopsReader = StopsReaderAggregator.CreateFrom(tdbs).UseCache();
            ConnectionEnumerator = ConnectionEnumeratorAggregator.CreateFrom(tdbs);
            ConnectionReader = ConnectionReaderAggregator.CreateFrom(tdbs);
            Profile = profile;


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
        /// This method is run synchronously, but could be parallelized
        /// </summary>
        /// <returns></returns>
        public WithProfile<T> PrecalculateClosestStops()
        {
            Log.Information("Caching reachable locations");
            var start = DateTime.Now;
            StopsReader.Reset();
            while (StopsReader.MoveNext())
            {
                var current = (IStop) StopsReader;
                StopsReader.LocationsInRange(
                    current.Latitude, current.Longitude,
                    Profile.WalksGenerator.Range());
            }

            var end = DateTime.Now;
            Log.Information($"Caching reachable locations took {(end - start).TotalMilliseconds}ms");
            return this;
        }


        public IWithSingleLocation<T> SelectSingleStop(IEnumerable<(LocationId, Journey<T>)> stop)
        {
            return new WithLocation<T>(StopsReader, ConnectionEnumerator, ConnectionReader, Profile, stop,
                stop);
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
        public IWithSingleLocation<T> SelectSingleStop(IEnumerable<LocationId> stop)
        {
            return SelectSingleStop(AddNullJourneys(stop));
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
                StopsReader.FindStops(stop, f => $"Stop {f} was not found")
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
        public IWithSingleLocation<T> SelectSingleStop(IEnumerable<IStop> stop)
        {
            return SelectSingleStop(
                stop.Select(f => f.Id)
            );
        }

        public IWithSingleLocation<T> SelectSingleStop(LocationId stop)
        {
            return SelectSingleStop(new List<LocationId> {stop});
        }

        public IWithSingleLocation<T> SelectSingleStop(IStop stop)
        {
            return SelectSingleStop(stop.Id);
        }

        public IWithSingleLocation<T> SelectSingleStop(string stop)
        {
            return SelectSingleStop(
                StopsReader.FindStop(stop, $"Stop {stop} was not found")
            );
        }


        public WithLocation<T> SelectStops(IEnumerable<(LocationId, Journey<T>)> from,
            IEnumerable<(LocationId, Journey<T>)> to)
        {
            return new WithLocation<T>(StopsReader, ConnectionEnumerator, ConnectionReader, Profile, @from, to);
        }

        public WithLocation<T> SelectStops(IEnumerable<LocationId> from,
            IEnumerable<LocationId> to)
        {
            return SelectStops(AddNullJourneys(from), AddNullJourneys(to));
        }

        public WithLocation<T> SelectStops(IEnumerable<string> from, IEnumerable<string> to)
        {
            return SelectStops(
                StopsReader.FindStops(from, f => $"Departure stop {f} was not found"),
                StopsReader.FindStops(to, t => $"Arrival stop {t} was not found")
            );
        }

        public WithLocation<T> SelectStops(IEnumerable<IStop> from, IEnumerable<IStop> to)
        {
            return SelectStops(
                from.Select(f => f.Id),
                to.Select(t => t.Id)
            );
        }

        public WithLocation<T> SelectStops(LocationId from, LocationId to)
        {
            return SelectStops(new List<LocationId> {from}, new List<LocationId> {to});
        }

        public WithLocation<T> SelectStops(IStop from, IStop to)
        {
            return SelectStops(from.Id, to.Id);
        }

        public WithLocation<T> SelectStops(string from, string to)
        {
            return SelectStops(
                StopsReader.FindStop(from, $"Departure stop {from} was not found"),
                StopsReader.FindStop(to, $"Arrival stop {to} was not found")
            );
        }


        private static List<(LocationId, Journey<T>)> AddNullJourneys(IEnumerable<LocationId> locs)
        {
            var l = new List<(LocationId, Journey<T>)>();
            foreach (var loc in locs)
            {
                l.Add((loc, null));
            }

            return l;
        }
    }

    public interface IWithSingleLocation<T> where T : IJourneyMetric<T>
    {
        IWithTimeSingleLocation<T> SelectTimeFrame(
            DateTime start,
            DateTime end);

        IWithTimeSingleLocation<T> SelectTimeFrame(
            ulong start,
            ulong end);
    }

    public class WithLocation<T> : IWithSingleLocation<T>
        where T : IJourneyMetric<T>
    {
        internal readonly IStopsReader StopsReader;
        internal readonly IConnectionEnumerator ConnectionEnumerator;
        internal readonly IConnectionReader ConnectionReader;


        private readonly Profile<T> _profile;

        private readonly List<(LocationId, Journey<T>)> _from;
        private readonly List<(LocationId, Journey<T>)> _to;


        internal WithLocation(IStopsReader stopsReader,
            IConnectionEnumerator connectionEnumerator,
            IConnectionReader connectionReader,
            Profile<T> profile,
            IEnumerable<(LocationId, Journey<T>)> @from, IEnumerable<(LocationId, Journey<T>)> to)
        {
            _profile = profile;
            ConnectionEnumerator = connectionEnumerator;
            ConnectionReader = connectionReader;
            StopsReader = stopsReader;
            _from = from.ToList();
            _to = to.ToList();
        }


        public WithTime<T> SelectTimeFrame(
            DateTime start,
            DateTime end)
        {
            return new WithTime<T>(StopsReader, ConnectionEnumerator, ConnectionReader,_profile, _from, _to, start, end);
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
    }

    public interface IWithTimeSingleLocation<T> where T : IJourneyMetric<T>
    {
        ///  <summary>
        ///  Calculates all journeys which depart at 'from' at the given departure time and arrive before the specified 'end'-time of the timeframe.
        ///  </summary>
        IReadOnlyDictionary<LocationId, Journey<T>> IsochroneFrom();

        ///  <summary>
        ///  Calculates all journeys which arrive at 'to' at the given arrival time and departarter the specified 'start'-time of the timeframe.
        ///  </summary>
        IReadOnlyDictionary<LocationId, Journey<T>> IsochroneTo();
    }

    public class WithTime<T> : IWithTimeSingleLocation<T>
        where T : IJourneyMetric<T>
    {
        internal readonly IStopsReader StopsReader;
        internal readonly IConnectionReader ConnectionReader;
        internal readonly IConnectionEnumerator ConnectionEnumerator;

        internal readonly Profile<T> Profile;
        internal readonly List<(LocationId, Journey<T>)> From;
        internal readonly List<(LocationId, Journey<T>)> To;

        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }


        private IConnectionFilter _filter;

        internal WithTime(IStopsReader stopsReader,
            IConnectionEnumerator connectionEnumerator,
            IConnectionReader connectionReader,
            Profile<T> profile,
            List<(LocationId, Journey<T>)> from,
            List<(LocationId, Journey<T>)> to,
            DateTime start,
            DateTime end)
        {
            StopsReader = stopsReader;
            ConnectionEnumerator = connectionEnumerator;
            ConnectionReader = connectionReader;
            Profile = profile;
            From = from;
            To = to;
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
        /// Creates a copy of this withTime-object, but with different times.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public WithTime<T> DifferentTimes(DateTime start, DateTime end)
        {
            return new WithTime<T>(
                StopsReader,
                ConnectionEnumerator,
                ConnectionReader,
                Profile,
                From,
                To,
                start,
                end
            );
        }

        ///  <summary>
        ///  Calculates all journeys which depart at 'from' at the given departure time and arrive before the specified 'end'-time of the timeframe.
        /// This ignores the given 'to'-location
        ///  </summary>
        public IReadOnlyDictionary<LocationId, Journey<T>> IsochroneFrom()
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
                StopsReader,
                ConnectionEnumerator,
                Start, End,
                Profile.MetricFactory,
                Profile.ProfileComparator,
                Profile.InternalTransferGenerator,
                Profile.WalksGenerator,
                From,
                new List<(LocationId, Journey<T>)>() // EMPTY LIST
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
        public IReadOnlyDictionary<LocationId, Journey<T>> IsochroneTo()
        {
            CheckHasTo();
            /*
             * Same principle as the other IsochroneFunction
             */
            var settings = new ScanSettings<T>(
                StopsReader,
                ConnectionEnumerator,
                Start,
                End,
                Profile.MetricFactory,
                Profile.ProfileComparator,
                Profile.InternalTransferGenerator,
                Profile.WalksGenerator,
                new List<(LocationId, Journey<T>)>(), // EMPTY LIST
                To
            );
            var las = new LatestConnectionScan<T>(settings);
            las.CalculateJourney();
            UseFilter(las.AsFilter());
            return las.Isochrone();
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
                StopsReader,
                ConnectionEnumerator,
                Start,
                End,
                Profile.MetricFactory,
                Profile.ProfileComparator,
                Profile.InternalTransferGenerator,
                Profile.WalksGenerator,
                From, To
            )
            {
                Filter =  _filter
            };
        }

        ///  <summary>
        ///  Calculates the journey which departs at 'from' and arrives at 'to' as early as possible.
        ///  </summary>
        /// <param name="expandSearch">
        /// EAS can be used to speed up PCS later on. However, PCS is often given a bigger time range to search.
        /// This function indicates a new timeframe-end to calculate PCS with later on.</param>
        /// <returns>A journey which is guaranteed to arrive as early as possible (or null if none was found)</returns>
        // ReSharper disable once UnusedMember.Global
        public Journey<T> EarliestArrivalJourney(
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
        public Journey<T> LatestDepartureJourney(
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
        ///
        /// Note that this list might contain families of very similar journeys, e.g. journeys which differ only in the transfer station taken.
        /// To prune them, use `PruneInAlternatives`
        /// </summary>
        public List<Journey<T>> AllJourneys()
        {
            CheckAll();
            var settings = GetScanSettings();
            var pcs = new ProfiledConnectionScan<T>(settings);
            return pcs.CalculateJourneys();
        }


        /// <summary>
        /// Calculates all journeys which are optimal for their given timeframe and which go to the destination stop.
        /// </summary>
        public Dictionary<LocationId, List<Journey<T>>> AllJourneysProfileTo()
        {
            CheckAll();
            var settings = new ScanSettings<T>(
                StopsReader,
                ConnectionEnumerator,
                Start,
                End,
                Profile.MetricFactory,
                Profile.ProfileComparator,
                Profile.InternalTransferGenerator,
                Profile.WalksGenerator,
                new List<(LocationId, Journey<T>)>(), // We don't pass any departure stop, as we want them all
                To
            )
            {
                Filter = _filter
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
        private void UseFilter(IConnectionFilter filter)
        {
            _filter = filter;
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

        private void CheckAll()
        {
            CheckHasFrom();
            CheckHasTo();
            CheckNoOverlap();
        }

        internal void ResetFilter()
        {
            _filter = null;
        }
    }
}