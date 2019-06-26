using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.OtherMode;

namespace Itinero.Transit.Algorithms.CSA
{
    internal static class ParetoExtensions
    {
        ///  <summary>
        ///  Creates a new pareto-frontier, based on the given frontier.
        ///  Every journey in the given frontier is backwards-extended with the connection c,
        ///  eventually with a fitting transfer-element in between as specified by the transfer policy.
        /// 
        ///  The resulting pareto frontier will only contain non-dominated journeys containing C.
        ///  
        ///  </summary>
        ///  <param name="pareto"></param>
        /// <param name="stopsReader"></param>
        /// <param name="c"></param>
        /// <param name="transferPolicy"></param>
        /// <typeparam name="T"></typeparam>
        ///  <returns></returns>
        public static ParetoFrontier<T> ExtendFrontierBackwards<T>(this ParetoFrontier<T> pareto,
            IStopsReader stopsReader,
            IConnection c, IOtherModeGenerator transferPolicy) where T : IJourneyMetric<T>
        {
            // The journeys in the frontier are ordered: the journeys which are added later to the profile,
            // will have an _earlier_ departure time (because PCS) runs in reverse order.
            var newFrontier = new ParetoFrontier<T>(pareto.Comparator, pareto.JourneyFilter);

            foreach (var journey in pareto.Frontier)
            {
                if (journey.Time <= c.ArrivalTime)
                {
                    // We can not hop on this journey
                    continue;
                }

                Journey<T> extendedJourney;
                var lastTripId = journey.LastTripId();
                if (lastTripId.HasValue && lastTripId.Value.Equals(c.TripId))
                {
                    extendedJournlastSey = journey.ChainBackward(c);
                }
                else
                {
                    extendedJourney =
                        journey
                            .ChainBackwardWith(stopsReader, transferPolicy, c.ArrivalStop)
                            ?.ChainBackward(c);
                }

                if (extendedJourney != null)
                {
                    // Transferring fails if there is too little time to make the transfer, or
                    // when the journey departs before we arrive in the station
                    newFrontier.AddToFrontier(extendedJourney);
                }
            }

            return newFrontier;
        }


        /// <summary>
        /// CAN CHANGE BOTH A & B!
        ///
        /// Adds all journeys of one frontier into the other frontier.
        ///Returns the frontier where everything was added.
        /// Note: which frontier is picked, is undefined an depends on the input
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static ParetoFrontier<T> Combine<T>(ParetoFrontier<T> a, ParetoFrontier<T> b) where T : IJourneyMetric<T>
        {
            var smallest = a;
            var biggest = b;
            if (smallest.Frontier.Count > biggest.Frontier.Count)
            {
                biggest = a;
                smallest = b;
            }


            // AddAllToFrontier uses 'yield return'.
            // Consuming the enumerator with 'toList' makes sure every yield is executed and thus that every journey is added
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            biggest.AddAllToFrontier(smallest.Frontier).ToList();

            return biggest;
        }
    }
}