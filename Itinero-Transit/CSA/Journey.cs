using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// A journey is a part in an intermodal trip, describing the route the user takes.
    /// </summary>
    public class Journey<T> where T : IJourneyStats<T>
    {
        public static readonly Journey<T> InfiniteJourney = new Journey<T>();


        public static readonly Comparer<Journey<T>> TimeComparer = new CompareTime<T>();
        public static readonly Comparer<Journey<T>> TimeCompareDesc = new CompareTimeDesc<T>();

        /// <summary>
        /// The previous link in this journey. Can be null if this is where we start the journey
        /// </summary>
        public Journey<T> PreviousLink { get; }


        /// <summary>
        /// The time that the journey starts or ends, depending on the used algorithm
        /// </summary>
        public DateTime Time { get; }

        /// <summary>
        /// The connection taken for this journey
        /// </summary>
        public IConnection Connection { get; }

        /// <summary>
        /// Keeps some statistics about the journey
        /// </summary>
        public T Stats { get; }

        private Journey()
        {
            PreviousLink = null;
            Time = DateTime.MaxValue;
            Connection = null;
            Stats = default(T);
        }


        public Journey(Journey<T> previousLink, DateTime time, IConnection connection)
        {
            PreviousLink = previousLink;
            Time = time;
            Connection = connection;
            Stats = previousLink.Stats.Add(this);
        }

        public Journey(T singleConnectionStats, DateTime time, IConnection connection)
        {
            PreviousLink = null;
            Time = time;
            Connection = connection;
            Stats = singleConnectionStats;
        }


        public override string ToString()
        {
            var res = PreviousLink == null ? $"JOURNEY ({Time:O}): \n" : PreviousLink.ToString();

            res += "  "+(Connection == null ? "-- No connection given--" : Connection.ToString())+"\n";
            res += "  "+(Stats == null ? "-- No stats -- ": Stats.ToString())+"\n";
            return res;
        }
    }


    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    internal class CompareTime<T> : Comparer<Journey<T>>
        where T : IJourneyStats<T>
    {
        public override int Compare(Journey<T> x, Journey<T> y)
        {
            return x.Time.CompareTo(y.Time);
        }
    }

    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    internal class CompareTimeDesc<T> : Comparer<Journey<T>>
        where T : IJourneyStats<T>
    {
        public override int Compare(Journey<T> x, Journey<T> y)
        {
            return y.Time.CompareTo(x.Time);
        }
    }
}