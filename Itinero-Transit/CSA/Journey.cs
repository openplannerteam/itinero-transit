using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// A journey is a part in an intermodal trip, describing the route the user takes.
    ///
    /// Normally, a journey is constructed with the startlocation hidden the deepest in the data structure.
    /// The Time is mostly the arrival time.
    ///
    /// The above properties are reversed in the CPS algorithm. The last step of that algorithm is to reverse the journeys,
    /// so that users of the lib get a uniform experience
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
            if (connection == null)
            {
                throw new ArgumentException("The connection used to initialize a Journey should not be null");
            }

            if (time == null)
            {
                throw new ArgumentException("The Time used to initialize a Journey should not be null");
            }

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

        /// <summary>
        /// Creates a new journey which goes in the opposite direction.
        /// The 'Time' field will be set to the arrival time of the total journey.
        /// </summary>
        /// <returns></returns>
        public Journey<T> Reverse()
        {
            // We start with the current element, which will be the last connection
            var reversed = new Journey<T>(Stats.InitialStats(Connection), Connection.ArrivalTime(), Connection);

            var current = PreviousLink;
            while (current != null)
            {
                reversed = new Journey<T>(reversed, current.Connection.ArrivalTime(), current.Connection);
                current = current.PreviousLink;
            }

            return reversed;
        }


        public override string ToString()
        {
            var res = PreviousLink == null ? $"JOURNEY ({Time:O}): \n" : PreviousLink.ToString();

            res += "  " + (Connection == null ? "-- No connection given--" : Connection.ToString()) + "\n";
            res += "  " + (Stats == null ? "-- No stats -- " : Stats.ToString()) + "\n";
            return res;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Journey<T> j))
            {
                return false;
            }

            return Equals(j);
        }

        protected bool Equals(Journey<T> other)
        {
            var b = (Time == null ? other.Time == null : Time.Equals(other.Time))
                    && (Connection == null ? other.Connection == null : Connection.Equals(other.Connection))
                    && Equals(PreviousLink, other.PreviousLink);
            return b;
        }

        /// <summary>
        /// Gets the first journey in the chain
        /// </summary>
        /// <returns></returns>
        public Journey<T> First()
        {
            return PreviousLink?.First() ?? this;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (PreviousLink != null ? PreviousLink.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Time.GetHashCode();
                hashCode = (hashCode * 397) ^ (Connection != null ? Connection.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Stats);
                return hashCode;
            }
        }

        public void VerboseDiff(Journey<T> j)
        {
            if (!Equals(Connection, j.Connection))
            {
                throw new ArgumentException($"Not the same: \n{Connection}\n{j.Connection}");
            }

            PreviousLink.VerboseDiff(j.PreviousLink);
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