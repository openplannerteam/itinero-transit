using System.Collections.Generic;

namespace Itinero.Transit.Journeys
{
    // ReSharper disable once UnusedMember.Global
    public static class JourneyExtensions
    {
        public static List<Journey<T>> AllParts<T>(this Journey<T> j) where T : IJourneyStats<T>
        {
            var parts = new List<Journey<T>>();
            var current = j;
            do
            {
                parts.Add(current);
                current = current.PreviousLink;
            } while (current != null && !ReferenceEquals(current, current.PreviousLink));

            return parts;
        }

        internal static List<Journey<T>> Reversed<T>(this Journey<T> j) where T : IJourneyStats<T>
        {
            var l = new List<Journey<T>>();
            Reversed(j, l);
            return l;
        }

        /// <summary>
        /// Reverses and flattens the journey.
        /// The resulting, new journeys will not contain alternative choices and will be added to the list
        /// </summary>
        /// <returns></returns>
        internal static void Reversed<T>(this Journey<T> j, List<Journey<T>> addTo) where T : IJourneyStats<T>
        {
            Reversed(j, new Journey<T>(j.Location, j.Time, j.Stats.EmptyStat(), j.Root.TripId), addTo);
        }

        private static void Reversed<T>(this Journey<T> j, Journey<T> buildOn, List<Journey<T>> addTo)
            where T : IJourneyStats<T>
        {
            if (j.SpecialConnection && j.Connection == Journey<T>.GENESIS)
            {
                // We have arrived at the end of the journey, all information should be added already
                addTo.Add(buildOn);
                return;
            }

            if (j.SpecialConnection && j.Connection == Journey<T>.JOINED_JOURNEYS)
            {
                j.PreviousLink.Reversed(buildOn, addTo);
                j.AlternativePreviousLink.Reversed(buildOn, addTo);
                return;
            }

            if (j.SpecialConnection)
            {
                buildOn = buildOn.ChainSpecial(
                    j.Connection, j.PreviousLink.Time, j.PreviousLink.Location,
                    j.TripId);
            }
            else
            {
                buildOn = buildOn.Chain(
                    j.Connection, j.PreviousLink.Time, j.PreviousLink.Location,
                    j.TripId);
            }


            j.PreviousLink.Reversed(buildOn, addTo);
        }


        public static Journey<T> Pruned<T>(this Journey<T> j) where T : IJourneyStats<T>
        {
            var restOfTheJourney = j.PreviousLink.PrunedWithoutLast();
            return restOfTheJourney.Chain(j.Connection, j.Time, j.Location, j.TripId);
        }

        /// <summary>
        /// Creates a new journey, where only important stops are retained. The intermediate stops are scrapped
        /// </summary>
        /// <returns></returns>
        private static Journey<T> PrunedWithoutLast<T>(this Journey<T> j) where T : IJourneyStats<T>
        {
            if (j.SpecialConnection && j.Connection == Journey<T>.GENESIS)
            {
                return j;
            }

            if (j.SpecialConnection)
            {
                var restOfTheJourney = j.PreviousLink.Pruned();
                return restOfTheJourney.ChainSpecial(
                    j.Connection, j.Time, j.Location, j.PreviousLink.TripId);
            }

            return j.PreviousLink.PrunedWithoutLast();
        }


        /// <summary>
        /// Given a journey and a reversed journey, append the reversed journey to the journey
        /// </summary>
        public static Journey<T> Append<T>(this Journey<T> j, Journey<T> restingJourney) where T : IJourneyStats<T>
        {

            if (restingJourney == null)
            {
                return j;
            }

            // ReSharper disable once PossibleUnintendedReferenceComparison
            while (restingJourney.PreviousLink != restingJourney)
            {
                var timeDiff = restingJourney.PreviousLink.Time - restingJourney.Time;
                j = j.Chain(
                    restingJourney.Connection,
                    j.Time + timeDiff,
                    restingJourney.PreviousLink.Location, // restingJourney is backward, so will contain the departure location
                    restingJourney.TripId
                );
            }
            return j;
        }

        public static Journey<T> SetTag<T>(this Journey<T> j, uint tag) where T : IJourneyStats<T>
        {

            if (j.SpecialConnection && j.Connection == Journey<T>.GENESIS)
            {
                return new Journey<T>(j.Location, j.DepartureTime(), j.Stats, tag);
            }
            return j.PreviousLink.SetTag(tag).Chain(j.Connection, j.Time, j.Location, j.TripId);
        }

        /// <summary>
        /// Takes a journey with a statistics tracker T and applies a statistics tracker S to them
        /// The structure of the journey will be kept
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static Journey<S> MeasureWith<T, S>(this Journey<T> j, S newStatFactory)
            where S : IJourneyStats<S>
            where T : IJourneyStats<T>
        {
            if (j.PreviousLink == null)
            {
                // We have found the genesis
                return new Journey<S>(
                    j.Location, j.Time, newStatFactory.EmptyStat(), j.TripId);
            }

            if (j.SpecialConnection && j.Connection == Journey<T>.JOINED_JOURNEYS)
            {
                var prev = j.PreviousLink.MeasureWith(newStatFactory);
                var altPrev = j.AlternativePreviousLink?.MeasureWith(newStatFactory);
                return new Journey<S>(prev, altPrev);
            }



            if (j.SpecialConnection)
            {
                return j.PreviousLink.MeasureWith(newStatFactory)
                    .ChainSpecial(j.Connection, j.Time, j.Location, j.TripId); 
            }

            return j.PreviousLink.MeasureWith(newStatFactory).Chain(
                j.Connection, j.Time, j.Location, j.TripId);
            
        }
    }
}