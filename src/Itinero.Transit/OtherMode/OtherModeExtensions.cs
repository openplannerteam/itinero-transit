using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Journey;

namespace Itinero.Transit.OtherMode
{
    public static class OtherModeExtensions
    {
        public static OtherModeCacher UseCache(this IOtherModeGenerator fallback)
        {
            return new OtherModeCacher(fallback);
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


            var reachableLocations =
                stops.LocationsInRange(stops.Latitude, stops.Longitude, otherModeGenerator.Range());

            stops.MoveTo(journey.Location);
            var times = otherModeGenerator.TimesBetween( /*from Istop journey.Location*/stops, reachableLocations);

            foreach (var v in times)
            {
                var reachableLocation = v.Key;
                var time = v.Value;

                if (reachableLocation.Equals(location))
                {
                    continue;
                }

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
            StopId location,
            IOtherModeGenerator otherModeGenerator,
            IStopsReader stops) where T : IJourneyMetric<T>
        {
            if (!stops.MoveTo(location))
            {
                throw new ArgumentException($"Location {location} not found, could not move to it");
            }

            var l = new Stop(stops);
            var reachableLocations =
                stops.LocationsInRange(l.Latitude, l.Longitude, otherModeGenerator.Range());

            var times = otherModeGenerator.TimesBetween(l, reachableLocations);

            foreach (var j in journeys)
            {
                foreach (var v in times)
                {
                    var reachableLocation = v.Key;
                    var time = v.Value;

                    if (reachableLocation.Equals(location))
                    {
                        continue;
                    }

                    // Biggest difference: subtraction instead of addition
                    var walkingJourney =
                        j.ChainSpecial(Journey<T>.OTHERMODE, j.Time - time, reachableLocation,
                            new TripId(otherModeGenerator));

                    yield return walkingJourney;
                }
            }
        }

        /// <summary>
        /// A very straightforward implementation to get multiple routings at the same time...
        /// 
        /// </summary>
        internal static Dictionary<StopId, uint> DefaultTimesBetween(
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
    }
}