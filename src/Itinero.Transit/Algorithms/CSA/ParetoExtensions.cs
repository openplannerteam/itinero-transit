using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

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
        ///  <param name="c"></param>
        /// <param name="transferPolicy"></param>
        /// <typeparam name="T"></typeparam>
        ///  <returns></returns>
        public static ParetoFrontier<T> ExtendFrontier<T>(this ParetoFrontier<T> pareto,
            IConnection c, IOtherModeGenerator transferPolicy) where T : IJourneyStats<T>
        {
            // The journeys in the frontier are ordered: the journeys which are added later to the profile,
            // will have an _earlier_ departure time (because PCS) runs in reverse order.
            var newFrontier = new ParetoFrontier<T>(pareto.Comparator);

            foreach (var journey in pareto.Frontier)
            {
                if (journey.Time <= c.ArrivalTime)
                {
                    // We can not hop on this journey
                    continue;
                }

                Journey<T> extendedJourney;
                if (journey.LastTripId() == c.TripId)
                {
                    extendedJourney = journey.ChainBackward(c);
                }
                else
                {
                    extendedJourney = transferPolicy
                        .CreateArrivingTransfer(journey, c.ArrivalTime, c.ArrivalStop)
                        ?.ChainBackward(c);
                }

                if (extendedJourney != null)
                {
                    // Transferring fails if there is too little time to make the transfer, or
                    // when the journey departs before we arrive in the station
                    newFrontier.AddToFrontier(extendedJourney);
                }

                // TODO optimize this
                // At a certain point, all additions will fail: the traveltime will grow to long and the journey won't have something extra to offer
                // We must be able to detect this and break the loop
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
        public static ParetoFrontier<T> Combine<T>(ParetoFrontier<T> a, ParetoFrontier<T> b) where T : IJourneyStats<T>
        {
            var smallest = a;
            var biggest = b;
            if (smallest.Frontier.Count > biggest.Frontier.Count)
            {
                biggest = a;
                smallest = b;
            }

            biggest.AddAllToFrontier(smallest.Frontier);

            return biggest;
        }
    }
}