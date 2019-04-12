using System;
using System.Collections.Generic;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Algorithms.CSA
{
    public class ParetoFrontier<T>
        where T : IJourneyMetric<T>

    {
        public readonly MetricComparator<T> Comparator;

        /// <summary>
        /// Contains all the points on the frontier, in order
        /// This is needed for certain optimisations.
        /// Note that most removals (if they happen) will probably be on the tail, so not have too much of an performance impact
        /// </summary>
        public readonly List<Journey<T>> Frontier = new List<Journey<T>>();


        public ParetoFrontier(MetricComparator<T> comparator)
        {
            Comparator = comparator ?? throw new ArgumentNullException(nameof(comparator),
                             "A Pareto Frontier can not operate without comparator");
        }


        /// <summary>
        /// Are the given metric on the current frontier?
        /// Returns false if they are outperformed   
        /// </summary>
        /// <param name="considered">The considered metrics</param>
        /// <returns></returns>
        public bool OnTheFrontier(Journey<T> considered)
        {
            foreach (var guard in Frontier)
            {
                var comparison = Comparator.ADominatesB(guard, considered);
                if (comparison < 0)
                {
                    // The new journey didn't make the cut
                    return false;
                }

                //  if (comparison == 1)
                // The new journey defeated the guard
                // If we were to add these metric to the frontier, the guard would be removed

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
            if (considered == null || ReferenceEquals(considered, Journey<T>.InfiniteJourney))
            {
                return false;
            }


            for (var i = Frontier.Count - 1; i >= 0; i--)
            {
                var guard = Frontier[i];
                var duel = Comparator.ADominatesB(guard, considered);
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

                        // The new journey might take another subpath, e.g; travel via another station but arrive at the same time
                        // We add it here, but... as the guard is just as optimal,
                        // we know that no other journey can dominate it nor can this new journey dominate any other journey
                        // So we add the journey immediately and return
                        // Frontier.Add(considered);

                        // Also: because we know both are equally good, we can merge them (the pareto profile is kept only for if we'd transfer)
                        // And as a pareto frontier is only used in PCS, that should be fine
                        Frontier[i] = new Journey<T>(guard, considered);

                        return true;
                }
            }

            /* TODO as soon as we know that we can add the journey (e.g. by defeating or being just as good as another journey)
              we only need to check the remaining frontier to remove now obsolete values
            */
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


        public override string ToString()
        {
            var result = $"Pareto frontier with {Frontier.Count} entries";

            foreach (var j in Frontier)
            {
                result += "\n" + j;
            }

            return result;
        }
    }
}