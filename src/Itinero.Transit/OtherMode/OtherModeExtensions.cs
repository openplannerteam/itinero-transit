using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey;

namespace Itinero.Transit.OtherMode
{
    public static class OtherModeExtensions
    {
        public static OtherModeCache UseCache(this IOtherModeGenerator fallback)
        {
            return new OtherModeCache(fallback);
        }

        public static uint TimeBetween(this IOtherModeGenerator modeGenerator, IStopsReader reader, StopId from,
            StopId to)
        {
            reader.MoveTo(from);
            var fr = new Stop(reader);
            reader.MoveTo(to);
            return modeGenerator.TimeBetween(fr, reader);
        }

        /// <summary>
        /// Uses the otherMode to 'walk' towards all the reachable stops from the arrival of the given journey.
        /// The given journey will be extended to 'n' journeys.
        /// Returns an empty list if no other stop is in range
        /// </summary>
        public static IEnumerable<Journey<T>> WalkAwayFrom<T>(
            this Journey<T> journey,
            IOtherModeGenerator otherModeGenerator,
            IStopsReader stops) where T : IJourneyMetric<T>
        {
            var location = journey.Location;
            if (!stops.MoveTo(location))
            {
                throw new ArgumentException($"Location {location} not found, could not move to it");
            }

            var self = new Stop(stops);
            var reachableLocations =
                stops.StopsAround(self, otherModeGenerator.Range());

            stops.MoveTo(journey.Location);
            var times = otherModeGenerator.TimesBetween( /*from Istop journey.Location*/stops, reachableLocations);


            foreach (var v in times)
            {
                var reachableLocation = v.Key;
                var time = v.Value;

                if (time == uint.MaxValue)
                {
                    continue;
                }

                // We come from the journey, and walks towards the reachable location
                var walkingJourney =
                    journey.ChainSpecial(Journey<T>.OTHERMODE, journey.Time + time, reachableLocation,
                        new TripId(otherModeGenerator));

                yield return walkingJourney;
            }
        }

        public static IEnumerable<Journey<T>> WalkTowards<T>(
            this Journey<T> journey,
            IOtherModeGenerator otherModeGenerator,
            IStopsReader stops) where T : IJourneyMetric<T>
        {
            return new[] {journey}.WalkTowards(journey.Location, otherModeGenerator, stops);
        }

        /// <summary>
        /// Uses the otherMode to 'walk' from all the reachable stops from the departure of the given journey.
        /// The given 'n' journeys will be prefixed with a walk to the  'm' reachable locations, resulting in (at most) 'n*m'-journeys.
        /// Returns an empty list if no other stop is in range.
        ///
        /// IMPORTANT: All the journeys should have the same (given) Location
        /// </summary>
        public static IEnumerable<Journey<T>> WalkTowards<T>(
            this IEnumerable<Journey<T>> journeys,
            StopId location, // The target location
            IOtherModeGenerator otherModeGenerator,
            IStopsReader stops) where T : IJourneyMetric<T>
        {
            if (!stops.MoveTo(location))
            {
                throw new ArgumentException($"Location {location} not found, could not move to it");
            }


            var to = new Stop(stops);
            var reachableLocations =
                stops.StopsAround(to, otherModeGenerator.Range());

            var times = otherModeGenerator.TimesBetween(reachableLocations, to);

            foreach (var j in journeys)
            {
                // So: what do we have:
                // A backwards journey j. The head element of j represents a departure:
                // If the traveller gets to j.Location before j.Time, they can continue their journey towards the departure

                foreach (var v in times)
                {
                    // We should arrive in 'to' at 'j.Time' if we walk from 'from'
                    var from = v.Key;
                    // Time needed to walk
                    var time = v.Value;

                    if (time == uint.MaxValue)
                    {
                        continue;
                    }


                    // Biggest difference: subtraction instead of addition
                    // We walk towards j.Location, which equals 'to' location

                    // Based on that, we create a new journey, with a walk on top
                    // The new 'arrive before' time is a little sooner - the time needed to walk
                    // The new 'arrive at to continue' is where we could walk from 

                    var walkingJourney =
                        j.ChainSpecial(
                            Journey<T>.OTHERMODE,
                            j.Time - time,
                            from,
                            new TripId(otherModeGenerator)
                        );
                    yield return walkingJourney;
                }
            }
        }

        /// <summary>
        /// A very straightforward implementation to get multiple routings at the same time...
        /// 
        /// </summary>
        public static Dictionary<StopId, uint> DefaultTimesBetween(
            this IOtherModeGenerator modeGenerator, IStop coorFrom,
            IEnumerable<IStop> to)
        {
            var times = new Dictionary<StopId, uint>();
            foreach (var stop in to)
            {
                var time = modeGenerator.TimeBetween(coorFrom, stop);
                if (time == uint.MaxValue)
                {
                    continue;
                }

                times.Add(stop.Id, time);
            }

            return times;
        }

        public static Dictionary<StopId, uint> DefaultTimesBetween(
            this IOtherModeGenerator modeGenerator, IEnumerable<IStop> from,
            IStop to)
        {
            var times = new Dictionary<StopId, uint>();
            foreach (var stop in from)
            {
                var time = modeGenerator.TimeBetween(stop, to);
                if (time == uint.MaxValue)
                {
                    continue;
                }

                times.Add(stop.Id, time);
            }

            return times;
        }

        public static Dictionary<(StopId from, StopId to), uint> TimesBetween(
            this IOtherModeGenerator gen,
            List<Stop> from, List<Stop> to)
        {
            var result = new Dictionary<(StopId @from, StopId to), uint>();

            if (from.Count < to.Count)
            {
                foreach (var fr in from)
                {
                    var times = gen.TimesBetween(fr, to);
                    foreach (var arr in times.Keys)
                    {
                        result.Add((fr.Id, arr), times[arr]);
                    }
                }
            }
            else
            {
                foreach (var t in to)
                {
                    var times = gen.TimesBetween(from, t);
                    foreach (var fr in times.Keys)
                    {
                        result.Add((fr, t.Id), times[fr]);
                    }
                }
            }

            return result;
        }
    }
}