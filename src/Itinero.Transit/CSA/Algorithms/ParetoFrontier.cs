using System.Collections.Generic;

namespace Itinero.Transit.CSA
{
    public class ParetoFrontier<T>
        where T : IJourneyStats<T>

    {
        private readonly StatsComparator<T> _comparator;

        public readonly HashSet<Journey<T>> Frontier = new HashSet<Journey<T>>();


        public ParetoFrontier(StatsComparator<T> comparator)
        {
            _comparator = comparator;
        }


        public bool OnTheFrontier(Journey<T> journey)
        {
            return OnTheFrontier(journey.Stats);
        }

        /// <summary>
        /// Are the given statistics on the current frontier?
        /// Returns false if they are outperformed   
        /// </summary>
        /// <param name="considered">The considered statistics</param>
        /// <returns></returns>
        public bool OnTheFrontier(T considered)
        {
            foreach (var guard in Frontier)
            {
                var comparison = _comparator.ADominatesB(guard.Stats, considered);
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
            var toRemove = new HashSet<Journey<T>>();
            foreach (var guard in Frontier)
            {
                var comparison = _comparator.ADominatesB(guard, considered);
                if (comparison < 0)
                {
                    // The new journey didn't make the cut
                    return false;
                }

                if (comparison == 1)
                {
                    // The new journey defeated the guard
                    toRemove.Add(guard);
                }

                //if (comparison == int.MaxValue)
                // Both are on the pareto front

                //if (comparison == 0)
                // Both are equally good
                // As both might leave at different hours, we add the new journey as well
            }


            foreach (var defeatedGuard in toRemove)
            {
                Frontier.Remove(defeatedGuard);
            }

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