using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// A journey is a part in an intermodal trip, describing the route the user takes.
    /// </summary>
    public class Journey
    {
        public static readonly Journey InfiniteJourney = new Journey((IJourneyStats) null, DateTime.MaxValue, null);


        public static readonly Comparer<Journey> TimeComparer = new CompareTime();
        public static readonly Comparer<Journey> TimeCompareDesc = new CompareTimeDesc();

        /// <summary>
        /// The previous link in this journey. Can be null if this is where we start the journey
        /// </summary>
        public Journey PreviousLink { get; }


        /// <summary>
        /// The time that the journey will ends
        /// </summary>
        public DateTime Time { get; }

        /// <summary>
        /// The connection taken for this journey
        /// </summary>
        public IConnection Connection { get; }

        /// <summary>
        /// Keeps some statistics about the journey
        /// </summary>
        public IJourneyStats Stats { get; }


        public Journey(Journey previousLink, DateTime time, IConnection connection)
        {
            PreviousLink = previousLink;
            Time = time;
            Connection = connection;
            Stats = previousLink.Stats.Add(this);
        }

        public Journey(IJourneyStats singleConnectionStats, DateTime time, IConnection connection)
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
    internal class CompareTime : Comparer<Journey>
    {
        public override int Compare(Journey x, Journey y)
        {
            return x.Time.CompareTo(y.Time);
        }
    }

    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    internal class CompareTimeDesc : Comparer<Journey>
    {
        public override int Compare(Journey x, Journey y)
        {
            return y.Time.CompareTo(x.Time);
        }
    }
}