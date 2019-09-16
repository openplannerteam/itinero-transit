using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Profiles.Lua.Osm;
using Itinero.Transit.Algorithms.Filter;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Data.Core;
using Itinero.Transit.IO.OSM.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Tests.Functional.Transfers;
using Itinero.Transit.Tests.Functional.Utils;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Tests.Functional.Regression
{
    public class ProductionServerMimickTest : FunctionalTestWithInput<(string from, string to, uint maxSearch)>
    {
        private readonly TransitDb _transitDb;
        private readonly DateTime? _departureTime;
        private readonly DateTime? _arrivalTime;


        public ProductionServerMimickTest(TransitDb transitDb, DateTime? departureTime, DateTime? arrivalTime)
        {
            _transitDb = transitDb;
            _departureTime = departureTime;
            _arrivalTime = arrivalTime;
        }

        protected override void Execute()
        {
            var profile = CreateProfile(Input.from, Input.to);

            var foundJourneys = BuildJourneys(profile, Input.from, Input.to, _departureTime, _arrivalTime, true);
            NotNull(foundJourneys);
            NotNull(foundJourneys.Item1);
            True(foundJourneys.Item1.Count > 0);
        }

        private (
            List<Journey<TransferMetric>>, DateTime start, DateTime end) BuildJourneys(
                Profile<TransferMetric> p,
                string from, string to, DateTime? departure,
                DateTime? arrival,
                bool multipleOptions)
        {
            departure = departure?.ToUniversalTime();
            arrival = arrival?.ToUniversalTime();

            var reader = GetStopsReader();
            var osmIndex = reader.DatabaseIndexes().Max() + 1u;

            var stopsReader = StopsReaderAggregator.CreateFrom(new List<IStopsReader>
            {
                reader,
                new OsmLocationStopReader(osmIndex, true),
            }).UseCache(); // We cache here only for this request- only case cache will be missed is around the new stop locations

            // Calculate the first and last miles, in order to
            // 1) Detect impossible routes
            // 2) cache them

            stopsReader.MoveTo(from);
            var fromStop = new Stop(stopsReader);

            stopsReader.MoveTo(to);
            var toStop = new Stop(stopsReader);

           DetectFirstMileWalks(p, stopsReader, fromStop, osmIndex, false, "departure");
           DetectFirstMileWalks(p, stopsReader, toStop, osmIndex, true, "arrival");

           stopsReader.MakeComplete();

            // Close the cache, cross-calculate everything
            // Then, the 'SearchAround'-queries will not be run anymore.

            if (departure == null && arrival == null)
            {
                throw new NullReferenceException("Both departure and arrival are null");
            }

            var precalculator =
                _transitDb
                    .SelectProfile(p)
                    .SetStopsReader(stopsReader)
                    .SelectStops(from, to);
            WithTime<TransferMetric> calculator;
            if (departure == null)
            {
                // Departure time is null
                // We calculate one with a latest arrival scan search
                calculator = precalculator.SelectTimeFrame(arrival.Value.AddDays(-1), arrival.Value);
                // This will set the time frame correctly
                var latest = calculator
                    .CalculateLatestDepartureJourney(tuple =>
                        tuple.journeyStart - DefaultSearchLengthSearcher(2, TimeSpan.FromHours(1))(tuple.journeyStart, tuple.journeyEnd));
                if (!multipleOptions)
                {
                    return (new List<Journey<TransferMetric>> {latest},
                        latest.Root.Time.FromUnixTime(), latest.Time.FromUnixTime());
                }
            }
            else if (arrival == null || !multipleOptions)
            {
                calculator = precalculator.SelectTimeFrame(departure.Value, departure.Value.AddDays(1));


                // We do an earliest arrival search in a timewindow of departure time -> latest arrival time (eventually with arrival + 1 day)
                // This scan is extended for some time, in order to have both
                // - the automatically calculated latest arrival time
                // - an isochrone line in order to optimize later on
                var earliestArrivalJourney = calculator.CalculateEarliestArrivalJourney(
                    tuple => tuple.journeyStart + DefaultSearchLengthSearcher(2, TimeSpan.FromHours(1))(tuple.journeyStart, tuple.journeyEnd));
                if (earliestArrivalJourney == null)
                {
                    return (new List<Journey<TransferMetric>>(),
                        DateTime.MaxValue, DateTime.MinValue);
                }

                if (!multipleOptions)
                {
                    return (new List<Journey<TransferMetric>> {earliestArrivalJourney},
                        earliestArrivalJourney.Root.Time.FromUnixTime(), earliestArrivalJourney.Time.FromUnixTime());
                }
            }
            else
            {
                calculator = precalculator.SelectTimeFrame(departure.Value, arrival.Value);
                // Perform isochrone to speed up 'all journeys'
                calculator.CalculateIsochroneFrom();
            }


            return (calculator.CalculateAllJourneys(), calculator.Start, calculator.End);
        }
        
        private static void DetectFirstMileWalks<T>(
            Profile<T> p,
            IStopsReader stops, Stop stop, uint osmIndex, bool isLastMile, string name) where T : IJourneyMetric<T>
        {
            var failIfNoneFound = stop.Id.DatabaseId == osmIndex;

            if (p.WalksGenerator.Range() == 0)
            {
                // We can't walk with the current settings
                return;
            }

            var inRange = stops.StopsAround(stop, p.WalksGenerator.Range()).ToList();
            if (inRange == null 
                || !inRange.Any() 
                || inRange.Count == 1 && inRange[0].Id.Equals(stop.Id))
            {
                if (!failIfNoneFound)
                {
                    return;
                }
                throw new ArgumentException(
                    $"Could not find a station that is range from the {name}-location {stop.GlobalId} within {p.WalksGenerator.Range()}m. This range is calculated 'as the  crows fly', try increasing the range of your walksGenerator");
            }

            var foundRoutes = isLastMile ? 
                p.WalksGenerator.TimesBetween(inRange, stop) :
                p.WalksGenerator.TimesBetween(stop, inRange);

            if (!failIfNoneFound)
            {
                return;
            }

            if (foundRoutes == null)
            {
                CreateAndThrowErrorMessage(p, stop, isLastMile, name, inRange);
                return;
            }

            if (!foundRoutes.Any())
            {
                CreateAndThrowErrorMessage(p, stop, isLastMile, name, inRange);
            }

            var allInvalid = true;
            foreach (var (_, distance) in foundRoutes)
            {
                if (distance != uint.MaxValue)
                {
                    allInvalid = false;
                    break;
                }
            }

            if (allInvalid)
            {
                CreateAndThrowErrorMessage(p, stop, isLastMile, name, inRange);
            }
            
        }

        private static void CreateAndThrowErrorMessage<T>(Profile<T> p, Stop stop, bool isLastMile,
            string name, List<Stop> inRange)
            where T : IJourneyMetric<T>
        {
            var w = p.WalksGenerator;


            var errors = new List<string>();
            foreach (var stp in inRange)
            {
                var gen = w.GetSource(stop.Id, stp.Id);
                if (isLastMile)
                {
                    gen = w.GetSource(stp.Id, stop.Id);
                }

                var errorMessage =
                    $"A route from/to {stp} should have been calculated with {gen.OtherModeIdentifier()}";

                if (gen is OsmTransferGenerator osm)
                {
                    // THIS IS ONLY THE ERROR CASE
                    // NO, this isn't cached, I know that - it doesn't matter
                    osm.CreateRoute((stop.Latitude, stop.Longitude), (stp.Latitude, stp.Longitude), out _,
                        out var errMessage);
                    errorMessage += " but it said " + errMessage;
                }

                if (gen is CrowsFlightTransferGenerator)
                {
                    errorMessage += " 'Too Far'";
                }

                errors.Add(errorMessage);
            }

            var allErrs = string.Join("\n ", errors);

            throw new ArgumentException(
                $"Could not find a route towards/from the {name}-location.\nThe used generator is {w.OtherModeIdentifier()}\n{inRange.Count} stations in range are known\n The location we couldn't reach is {stop.GlobalId}\n {allErrs}");
        }

        private Profile<TransferMetric> CreateProfile(
            string @from, string to,
            uint internalTransferTime = 180,
            bool allowCancelled = false
        )
        {
            var stops = GetStopsReader().AddOsmReader();

            stops.MoveTo(from);
            var fromId = stops.Id;
            stops.MoveTo(to);
            var toId = stops.Id;


            var walksGenerator = new FirstLastMilePolicy(
                new CrowsFlightTransferGenerator(0),
                new OsmTransferGenerator(RouterDbStaging.RouterDb, Input.maxSearch, OsmProfiles.Pedestrian).UseCache(),
                fromId,
                new OsmTransferGenerator(RouterDbStaging.RouterDb, Input.maxSearch, OsmProfiles.Pedestrian).UseCache(),
                toId
            ).UseCache();


            var internalTransferGenerator = new InternalTransferGenerator(internalTransferTime);


            return new Profile<TransferMetric>(internalTransferGenerator,
                walksGenerator,
                TransferMetric.Factory,
                TransferMetric.ParetoCompare,
                allowCancelled ? null : new CancelledConnectionFilter(),
                new MaxNumberOfTransferFilter(uint.MaxValue));
        }

        private IStopsReader GetStopsReader()
        {
            return StopsReaderAggregator.CreateFrom(
                new List<IStopsReader>
                {
                    _transitDb.Latest.StopsDb.GetReader()
                }).UseCache();
        }

        public static Func<DateTime, DateTime, TimeSpan> DefaultSearchLengthSearcher(
            double factor, TimeSpan minimumTime)
        {
            return (start, end) =>
            {
                var diff = (end - start) * factor;

                if (diff < minimumTime)
                {
                    diff = minimumTime;
                }

                return diff;
            };
        }
    }
}