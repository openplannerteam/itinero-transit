using System;
using System.Collections.Generic;
using Itinero_Transit.CSA.ConnectionProviders;

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
            Connection = connection ??
                         throw new ArgumentException("The connection used to initialize a Journey should not be null");
            Stats = previousLink.Stats.Add(this);
        }

        /// <summary>
        /// A genesis journey with an empty walking transfer
        /// </summary>
        /// <param name="genesisLocation"></param>
        /// <param name="genesisTime"></param>
        /// <param name="statsFactory"></param>
        public Journey(Uri genesisLocation, DateTime genesisTime, T statsFactory)
        {
            PreviousLink = null;
            Time = genesisTime;
            Connection = new WalkingConnection(genesisLocation, genesisTime);
            Stats = statsFactory.InitialStats(Connection);
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

        /// <summary>
        /// Returns a new Journey where continuous connections are moved as tightly as possible onto to the journey
        /// E.g.
        ///
        /// Journey is
        /// "walk from 10:00 till 10:05 to the platform"
        /// "take train XYZ at 10:15, arriving at station at 11:15"
        /// "walk from 11:30 till 11:35 to your final destination"
        ///
        /// this will be 'pruned' to
        /// "walk from 10:10 till 10:15 to the platform"
        /// "take train XYZ at 10:15, arriving at station at 11:15"
        /// "walk from 11:15 till 11:20 to your final destination"
        /// 
        /// Resulting in a somewhat less timeconsuming journey
        /// </summary>
        /// <returns></returns>
        public Journey<T> Prune()
        {
            throw new NotImplementedException(); // TODO
        }


        public override string ToString()
        {
            return ToString(null);
        }


        public string ToString(ILocationProvider locDecode)
        {
            var res = PreviousLink == null ? $"JOURNEY ({Time:O}): \n" :
                PreviousLink.ToString(locDecode);
            res += "  " + (Connection == null ? "-- No connection given--" : Connection.ToString(locDecode)) + "\n";
            res += "    " + (Stats == null ? "-- No stats -- " : Stats.ToString()) + "\n";
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

        private bool Equals(Journey<T> other)
        {
            var b = (Time.Equals(other.Time))
                    && (Connection?.Equals(other.Connection) ?? other.Connection == null)
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
    }
}