using System;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// A journey is a part in an intermodal trip, describing the route the user takes.
    /// </summary>
    public class Journey
    {
        public static readonly Journey InfiniteJourney = new Journey(null, DateTime.MaxValue, null);
        /// <summary>
        /// The previous link in this journey. Can be null if this is where we start the journey
        /// </summary>
        public Journey PreviousLink { get; }


        /// <summary>
        /// The time that the journey will ends
        /// </summary>
        public DateTime ArrivalTime { get; }

        /// <summary>
        /// The connection taken for this journey
        /// </summary>
        public Connection Connection { get; }


        public Journey(Journey previousLink, DateTime arrivalTime, Connection connection)
        {
            PreviousLink = previousLink;
            ArrivalTime = arrivalTime;
            Connection = connection;
        }


        public override string ToString()
        {
            var res = PreviousLink == null ? "JOURNEY: \n" : PreviousLink.ToString();

            res += $"  {Connection}\n";
            return res;
        }

        
    }
}