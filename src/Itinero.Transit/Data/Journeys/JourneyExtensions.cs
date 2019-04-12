using System.Collections.Generic;

namespace Itinero.Transit.Journeys
{
    // ReSharper disable once UnusedMember.Global
    public static class JourneyExtensions
    {
        public static List<Journey<T>> AllParts<T>(this Journey<T> j) where T : IJourneyMetric<T>
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

        internal static List<Journey<T>> Reversed<T>(this Journey<T> j) where T : IJourneyMetric<T>
        {
            var l = new List<Journey<T>>();
            ReverseAndAddTo(j, l);
            return l;
        }

        /// <summary>
        /// Reverses and flattens the journey.
        /// The resulting, new journeys will not contain alternative choices and will be added to the list
        /// </summary>
        /// <returns></returns>
        internal static void ReverseAndAddTo<T>(this Journey<T> j, List<Journey<T>> addTo) where T : IJourneyMetric<T>
        {
            Reversed(j, new Journey<T>(j.Location, j.Time, j.Metric.Zero(), j.Root.TripId), addTo);
        }

        private static void Reversed<T>(this Journey<T> j, Journey<T> buildOn, List<Journey<T>> addTo)
            where T : IJourneyMetric<T>
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


        public static Journey<T> Pruned<T>(this Journey<T> j) where T : IJourneyMetric<T>
        {
            var restOfTheJourney = j.PreviousLink.PrunedWithoutLast();
            return restOfTheJourney.Chain(j.Connection, j.Time, j.Location, j.TripId);
        }

        /// <summary>
        /// Creates a new journey, where only important stops are retained. The intermediate stops are scrapped
        /// </summary>
        /// <returns></returns>
        private static Journey<T> PrunedWithoutLast<T>(this Journey<T> j) where T : IJourneyMetric<T>
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


        public static Journey<T> SetTag<T>(this Journey<T> j, uint tag) where T : IJourneyMetric<T>
        {
            if (j.SpecialConnection && j.Connection == Journey<T>.GENESIS)
            {
                return new Journey<T>(j.Location, j.DepartureTime(), j.Metric, tag);
            }

            return j.PreviousLink.SetTag(tag).Chain(j.Connection, j.Time, j.Location, j.TripId);
        }

        /// <summary>
        /// Takes a journey with a metrics tracker T and applies a metrics tracker S to them
        /// The structure of the journey will be kept
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static Journey<S> MeasureWith<T, S>(this Journey<T> j, S newMetricFactory)
            where S : IJourneyMetric<S>
            where T : IJourneyMetric<T>
        {
            if (j.PreviousLink == null)
            {
                // We have found the genesis
                return new Journey<S>(
                    j.Location, j.Time, newMetricFactory.Zero(), j.TripId);
            }

            if (j.SpecialConnection && j.Connection == Journey<T>.JOINED_JOURNEYS)
            {
                var prev = j.PreviousLink.MeasureWith(newMetricFactory);
                var altPrev = j.AlternativePreviousLink?.MeasureWith(newMetricFactory);
                return new Journey<S>(prev, altPrev);
            }


            if (j.SpecialConnection)
            {
                return j.PreviousLink.MeasureWith(newMetricFactory)
                    .ChainSpecial(j.Connection, j.Time, j.Location, j.TripId);
            }

            return j.PreviousLink.MeasureWith(newMetricFactory).Chain(
                j.Connection, j.Time, j.Location, j.TripId);
        }
    }
}