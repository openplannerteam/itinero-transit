using System;
using System.Collections.Generic;

namespace Itinero.IO.LC
{
    /// <summary>
    /// A journey is a part in an intermodal trip, describing the route the user takes.
    ///
    /// Normally, a journey is constructed with the start location hidden the deepest in the data structure.
    /// The Time is mostly the arrival time.
    ///
    /// The above properties are reversed in the PCS algorithm. The last step of that algorithm is to reverse the journeys,
    /// so that users of the lib get a uniform experience.
    /// </summary>
    public class Journey<T> where T : IJourneyStats<T>
    {
        public static readonly Journey<T> InfiniteJourney = new Journey<T>();


        /// <summary>
        /// The previous link in this journey. Can be null if this is where we start the journey
        /// </summary>
        public Journey<T> PreviousLink { get; }


        /// <summary>
        /// The connection taken for this journey
        /// </summary>
        public IJourneyPart Connection { get; }

        /// <summary>
        /// Keeps some statistics about the journey
        /// </summary>
        public T Stats { get; }

        public readonly Journey<T> Root;

        private Journey()
        {
            PreviousLink = null;
            Connection = new WalkingConnection(null, DateTime.MaxValue);
            Stats = default(T);
            Root = this;
        }


        public Journey(Journey<T> previousLink, IJourneyPart connection)
        {
            PreviousLink = previousLink;
            Connection = connection ??
                         throw new ArgumentException("The connection used to initialize a Journey should not be null");
            Stats = previousLink.Stats.Add(this);
            Root = previousLink.Root;

            if (Equals(previousLink.Connection, connection))
            {
                throw new ArgumentException(
                    $"Seems like you chained a connection to itself. This is a bug.\n{connection}");
            }
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
            Connection = new WalkingConnection(genesisLocation, genesisTime);
            Stats = statsFactory.InitialStats(Connection);
            Root = this;
        }


        public Journey(T singleConnectionStats, IJourneyPart connection)
        {
            PreviousLink = null;
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Stats = singleConnectionStats;
            Root = this;
        }

        /// <summary>
        /// Creates a new journey which goes in the opposite direction.
        /// The 'Time' field will be set to the arrival time of the total journey.
        /// </summary>
        public Journey<T> Reverse()
        {
            // We start with the current element, which will be the last connection
            var reversed = new Journey<T>(Stats.InitialStats(Connection), Connection);

            var current = PreviousLink;
            while (current != null)
            {
                reversed = new Journey<T>(reversed, current.Connection);
                current = current.PreviousLink;
            }

            return reversed;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public List<IJourneyPart> AllJourneyParts()
        {
            if (PreviousLink == null)
            {
                return new List<IJourneyPart> {Connection};
            }

            var list = PreviousLink.AllJourneyParts();
            list.Add(Connection);
            return list;
        }

        public Route AsRoute(ILocationProvider locations)
        {
            var routes = new List<Result<Route>>();
            foreach (var con in AllJourneyParts())
            {
                routes.Add(new Result<Route>(con.AsRoute(locations)));
            }

            return routes.Concatenate().Value;
        }

        /// <summary>
        /// Returns the tripID of the current connection.
        /// If the current connection does not have a trip ID,
        /// returns the last trip of the previous link
        /// </summary>
        public Uri GetLastTripId()
        {
            return (Connection as IConnection)?.Trip() ??
                   PreviousLink?.GetLastTripId();
        }


        public override string ToString()
        {
            return ToString(null);
        }


        public string ToString(ILocationProvider locDecode)
        {
            var res = PreviousLink == null ? $"JOURNEY: \n" : PreviousLink.ToString(locDecode);
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
            var b = Equals(Connection, other.Connection)
                    && Equals(PreviousLink, other.PreviousLink);
            return b;
        }

        // ReSharper disable once UnusedMember.Global
        public string DebugEquality(Journey<T> other, int depth = 0)
        {
            var res = $"{depth}: Connections: {Equals(Connection, other.Connection)} Same: {Equals(this, other)}\n" +
                      $"    this.Prev {PreviousLink != null}; other.prev {other.PreviousLink != null}";
            if (PreviousLink == null || other.PreviousLink == null)
            {
                return res;
            }

            return res + "\n" + PreviousLink.DebugEquality(other.PreviousLink, depth + 1);
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
                hashCode = (hashCode * 397) ^ (Connection != null ? Connection.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Stats);
                return hashCode;
            }
        }
    }
}