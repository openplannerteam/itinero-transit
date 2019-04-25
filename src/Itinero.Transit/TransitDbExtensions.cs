using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Algorithms.Search;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Journeys;

// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit
{
    /// <summary>
    /// This class is the main entry point to request routes as library user.
    /// It exposes all the core algorithms in a consistent and easy to use way.
    /// </summary>
    public static class TransitDbExtensions
    {
        public static WithProfile<T> SelectProfile<T>(this List<TransitDb.TransitDbSnapShot> tdb, Profile<T> profile)
            where T : IJourneyMetric<T>
        {
            return new WithProfile<T>(tdb, profile);
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
        private readonly IEnumerable<TransitDb.TransitDbSnapShot> _tdbs;
        private readonly Profile<T> _profile;

        internal WithProfile(IEnumerable<TransitDb.TransitDbSnapShot> tdbs, Profile<T> profile)
        {
            _tdbs = tdbs;
            _profile = profile;
        }

        public WithLocation<T> SelectStops(IEnumerable<LocationId> from, IEnumerable<LocationId> to)
        {
            return new WithLocation<T>(_tdbs, _profile, from, to);
        }

        public WithLocation<T> SelectStops(IEnumerable<string> from, IEnumerable<string> to)
        {
            return SelectStops(
                _tdbs.FindStops(from, f => $"Departure stop {f} was not found"),
                _tdbs.FindStops(to, t => $"Arrival stop {t} was not found")
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
                _tdbs.FindStop(from, $"Departure stop {from} was not found"),
                _tdbs.FindStop(to, $"Arrival stop {to} was not found")
            );
        }
    }

    public class WithLocation<T> where T : IJourneyMetric<T>
    {
        private readonly IEnumerable<TransitDb.TransitDbSnapShot> _tdbs;
        private readonly Profile<T> _profile;

        private readonly List<LocationId> _from;
        private readonly List<LocationId> _to;


        internal WithLocation(
            IEnumerable<TransitDb.TransitDbSnapShot> tdbs,
            Profile<T> profile,
            IEnumerable<LocationId> from, IEnumerable<LocationId> to)
        {
            _from = from.ToList();
            _to = to.ToList();
            _profile = profile;
            _tdbs = tdbs;
        }


        public WithTime<T> SelectTimeFrame(
            DateTime start,
            DateTime end)
        {
            return new WithTime<T>(_tdbs, _profile, _from, _to, start, end);
        }
    }

    public class WithTime<T> where T : IJourneyMetric<T>
    {
        private readonly IEnumerable<TransitDb.TransitDbSnapShot> _tdbs;
        private readonly Profile<T> _profile;
        private readonly List<LocationId> _from;
        private readonly List<LocationId> _to;

        private DateTime _start;
        private DateTime _end;

        private IConnectionFilter _filter;

        internal WithTime(IEnumerable<TransitDb.TransitDbSnapShot> tdbs,
            Profile<T> profile,
            List<LocationId> from,
            List<LocationId> to,
            DateTime start,
            DateTime end)
        {
            _tdbs = tdbs;
            _profile = profile;
            _from = from;
            _to = to;
            _start = start;
            _end = end;
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
                _tdbs,
                _start, _end,
                _profile.MetricFactory,
                _profile.ProfileComparator,
                _profile.InternalTransferGenerator,
                _profile.WalksGenerator,
                _from,
                new List<LocationId>() // EMPTY LIST
            );
            var eas = new EarliestConnectionScan<T>(settings);
            eas.CalculateJourney();
            UseFilter(eas.AsFilter());
            return eas.Isochrone();
        }


        public IReadOnlyDictionary<LocationId, Journey<T>> IsochroneTo()
        {
            CheckHasTo();
            /*
             * Same principle as the other IsochroneFunction
             */
            var settings = new ScanSettings<T>(
                _tdbs,
                _start,
                _end,
                _profile.MetricFactory,
                _profile.ProfileComparator,
                _profile.InternalTransferGenerator,
                _profile.WalksGenerator,
                new List<LocationId>(), // EMPTY LIST
                _to
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

        ///  <summary>
        ///  Calculates the journey which departs at 'from' and arrives at 'to' as early as possible.
        ///  </summary>
        ///  <param name="from">Where the traveller starts</param>
        ///  <param name="to">WHere the traveller wishes to go to</param>
        /// <param name="expandSearch">
        /// EAS can be used to speed up PCS later on. However, PCS is often given a bigger timerange to search.
        /// This function indicates a new timeframe-end to calculate PCS with later on.</param>
        /// <returns>A journey which is guaranteed to arrive as early as possible (or null if none was found)</returns>
        // ReSharper disable once UnusedMember.Global
        public Journey<T> EarliestArrivalJourney(
            Func<(DateTime journeyStart, DateTime journeyEnd), DateTime> expandSearch = null)
        {
            CheckAll();

            var settings = new ScanSettings<T>(
                _tdbs,
                _start,
                _end,
                _profile.MetricFactory,
                _profile.ProfileComparator,
                _profile.InternalTransferGenerator,
                _profile.WalksGenerator,
                _from, _to
            );

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
                _end = expandSearch((journey.Root.Time.FromUnixTime(), journey.Time.FromUnixTime()));
            }

            return journey;
        }


        ///  <summary>
        ///  Calculates the journey which arrives at 'to' and departs at 'from' as late as possible.
        ///  </summary>
        ///  <param name="from">Where the traveller starts</param>
        ///  <param name="to">WHere the traveller wishes to go to</param>
        /// <param name="expandSearch">
        /// LAS can be used to speed up PCS later on. However, PCS is often given a bigger timerange to search.
        /// This function indicates a new timeframe-start to calculate PCS with later on.
        /// </param>
        /// <returns>A journey which is guaranteed to arrive as early as possible (or null if none was found)</returns>
        // ReSharper disable once UnusedMember.Global
        public Journey<T> LatestDepartureJourney(
            Func<(DateTime journeyStart, DateTime journeyEnd), DateTime> expandSearch = null)
        {
            CheckAll();
            var settings = new ScanSettings<T>(
                _tdbs,
                _start,
                _end,
                _profile.MetricFactory,
                _profile.ProfileComparator,
                _profile.InternalTransferGenerator,
                _profile.WalksGenerator,
                _from, _to
            );

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
                _start = expandSearch((journey.Root.Time.FromUnixTime(), journey.Time.FromUnixTime()));
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
            var settings = new ScanSettings<T>(
                _tdbs,
                _start,
                _end,
                _profile.MetricFactory,
                _profile.ProfileComparator,
                _profile.InternalTransferGenerator,
                _profile.WalksGenerator,
                _from, _to
            )
            {
                Filter = _filter
            };
            var pcs = new ProfiledConnectionScan<T>(settings);
            return pcs.CalculateJourneys();
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
            if (_from == null || _from.Count == 0 || _to == null || _to.Count == 0)
            {
                return;
            }

            var overlap = _from.FindAll(f => _to.Contains(f));
            if (overlap.Any())
            {
                throw new Exception($"A departure stop is also used as arrival stop: {string.Join(", ", overlap)}");
            }
        }

        private void CheckHasFrom()
        {
            if (_from == null || _from.Count == 0)
            {
                throw new Exception($"No departure stop(s) are given");
            }
        }

        private void CheckHasTo()
        {
            if (_to == null || _to.Count == 0)
            {
                throw new Exception($"No arrival stop(s) are given");
            }
        }

        private void CheckAll()
        {
            CheckHasFrom();
            CheckHasTo();
            CheckNoOverlap();
        }
    }
}