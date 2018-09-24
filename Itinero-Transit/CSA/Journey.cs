using System;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// A journey is a part in an intermodal trip, describing the route the user takes.
    /// </summary>
    public class Journey
    {
        public static readonly Journey InfiniteJourney = new Journey((IJourneyStats) null, DateTime.MaxValue, null);

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
        public Connection Connection { get; }

        /// <summary>
        /// Keeps some statistics about the journey
        /// </summary>
        public IJourneyStats Stats { get; }


        public Journey(Journey previousLink, DateTime time, Connection connection)
        {
            PreviousLink = previousLink;
            Time = time;
            Connection = connection;
            Stats = previousLink.Stats.Add(this);
        }

        public Journey(IJourneyStats singleConnectionStats, DateTime time, Connection connection)
        {
            PreviousLink = null;
            Time = time;
            Connection = connection;
            Stats = singleConnectionStats;
        }


        public override string ToString()
        {
            var res = PreviousLink == null ? "JOURNEY: \n" : PreviousLink.ToString();

            res += $"  {Connection}\n";
            res += $"  {Stats}\n";
            return res;
        }
    }
}