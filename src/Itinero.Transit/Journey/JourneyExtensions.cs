using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Journey
{
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
            Reversed(j, new Journey<T>(j.Location, j.Time, j.Metric.Zero(), j.Root.TripId /* <- This is the debug tag*/), addTo);
        }

        private static void Reversed<T>(this Journey<T> j, Journey<T> buildOn, ICollection<Journey<T>> addTo)
            where T : IJourneyMetric<T>
        {
            if (j.SpecialConnection && Equals(j.Connection, Journey<T>.GENESIS))
            {
                // We have arrived at the end of the journey, all information should be added already
                addTo.Add(buildOn);
                return;
            }

            if (j.AlternativePreviousLink != null)
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

        /// <summary>
        /// Creates a new journey, which gives an overview of the journey.
        /// (Thus: only one segment per vehicle, walk or transfer).
        /// Journeys should be built in a forward fashion and be deduplicated.
        /// </summary>
        /// <param name="j"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [Pure]
        public static Journey<T> Summarized<T>(this Journey<T> j) where T : IJourneyMetric<T>
        {
            var parts = j.ToList();
            var summarized =
                new Journey<T>(parts[0].Location, parts[0].Time, parts[0].Metric);

            var i = 1;
            while (i < parts.Count)
            {
                // We search something that should be summarized
                // The 'segment to summarize' will be between (and including) pDep and pEnd
                var pDep = parts[i];

                if (pDep.SpecialConnection)
                {
                    summarized = summarized.ChainSpecial(pDep.Connection, pDep.Time, pDep.Location, pDep.TripId);
                    i++;
                    continue;
                }

                do
                {
                    i++;
                } while (i < parts.Count && !parts[i].SpecialConnection);

                var pEnd = parts[i - 1];
                // pDep --> pEnd are just all part of the same trip
                // We summarize it as a single connection
                var connection = new Connection("summarized-connection",
                    pDep.Location, pEnd.Location,
                    pDep.PreviousLink.Time, (ushort) (pEnd.Time - pDep.PreviousLink.Time),
                    0, 0, 0, pDep.TripId);

                var connectionId = pDep.Connection;
                
                summarized = summarized.ChainForward(connectionId, connection);
            }

            return summarized;
        }


        /// <summary>
        /// Generates a list with all the segments of the journey, for easy indexing.
        /// Element [0] is the root
        /// </summary>
        /// <returns></returns>
        [Pure]
        public static List<Journey<T>> ToList<T>(this Journey<T> j) where T : IJourneyMetric<T>
        {
            var allElements = new List<Journey<T>>();

            while (j != null)
            {
                allElements.Add(j);
                j = j.PreviousLink;
            }

            allElements.Reverse();

            return allElements;
        }

        /// <summary>
        /// Counts how many journeys this journey-family represents
        /// </summary>
        /// <param name="j"></param>
        /// <returns></returns>
        [Pure]
        public static uint CountLeaves<T>(this Journey<T> j) where T : IJourneyMetric<T>
        {
            if (j.PreviousLink == null)
            {
                return 1;
            }

            return (j.AlternativePreviousLink?.CountLeaves() ?? 0) + j.PreviousLink.CountLeaves();
        }
        
        [Pure]
        public static Journey<T> SetTag<T>(this Journey<T> j, TripId tag) where T : IJourneyMetric<T>
        {
            if (j.SpecialConnection && Equals(j.Connection, Journey<T>.GENESIS))
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
        [Pure]
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

            if (j.AlternativePreviousLink != null)
            {
               throw new ArgumentException("Journey still contains an alternative previous link. Please dedup before using this methods");
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