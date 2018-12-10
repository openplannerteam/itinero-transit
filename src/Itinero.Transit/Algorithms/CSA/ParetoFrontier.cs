using System.Collections.Generic;
using Itinero.Transit.Journeys;

namespace Itinero.IO.LC
{
    public class ParetoFrontier<T>
        where T : IJourneyStats<T>

    {
        private readonly StatsComparator<T> _comparator;

        /// <summary>
        /// Contains all the points on the frontier, in order
        /// This is needed for certain optimisations.
        /// Note that most removals (if they happen) will probably be on the tail, so not have too much of an performance impact
        /// </summary>
        public readonly List<Journey<T>> Frontier = new List<Journey<T>>();


        public ParetoFrontier(StatsComparator<T> comparator)
        {
            _comparator = comparator;
        }



        /// <summary>
        /// Are the given statistics on the current frontier?
        /// Returns false if they are outperformed   
        /// </summary>
        /// <param name="considered">The considered statistics</param>
        /// <returns></returns>
        public bool OnTheFrontier(Journey<T> considered)
        {
            foreach (var guard in Frontier)
            {
                var comparison = _comparator.ADominatesB(guard, considered);
                if (comparison < 0)
                {
                    // The new journey didn't make the cut
                    return false;
                }

                //  if (comparison == 1)
                // The new journey defeated the guard
                // If we were to add these stats to the frontier, the guard would be removed

                //if (comparison == int.MaxValue)
                // Both are on the pareto front

                //if (comparison == 0)
                // Both are equally good
                // As both might leave at different hours, we add the new journey as well
            }

            return true;
        }

        /// <summary>
        /// If the given journey is pareto-optimal in respect to the current frontier,
        /// the journey is added.
        /// If this journey outperforms some other point on the frontier, that point is removed
        /// </summary>
        /// <param name="considered"></param>
        /// <returns>True if the journey was appended to the frontier</returns>
        public bool AddToFrontier(Journey<T> considered)
        {
            for (var i = Frontier.Count - 1; i >= 0; i--)
            {
                var guard = Frontier[i];
                var duel = _comparator.ADominatesB(guard, considered);
                switch (duel)
                {
                    case -1:
                        // The new journey didn't make the cut
                        return false;
                    case 1:
                        // The new journey defeated the guard
                        Frontier.RemoveAt(i);
                        i--;
                        break;
                    case 0:
                        // Both are equally good
                        // They might be the same. We don't care about duplicates, so...
                        if (considered.Equals(guard))
                        {
                            return false;
                        }

                        // As both might leave at different hours, we add the new journey as well... except if they are the same ofc
                        break;
                }
            }

            //if (comparison == int.MaxValue)
            // Both are on the pareto front
            Frontier.Add(considered);
            return true;
        }

        /// <summary>
        /// Considers all of the journeys to append them to the frontier
        /// </summary>
        public void AddAllToFrontier(IEnumerable<Journey<T>> journeys)
        {
            foreach (var journey in journeys)
            {
                AddToFrontier(journey);
            }
        }
    }
}
