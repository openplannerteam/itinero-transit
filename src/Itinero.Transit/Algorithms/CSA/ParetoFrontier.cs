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
                        continue; // We continue the loop to remove other, possible sub-optimal entries further ahead in the list
                    case 0: // Both have exactly the same stats...
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
                    case int.MaxValue: // Both are better then the other on some different statistic
                        // So: 1) The guard can not eliminate the candidate
                        // 2) The candidate can not eliminate the guard 
                        // We just have to continue scanning - if no guard defeats the candidate, it owned its place
                        continue;
                    default:
                        throw new Exception("Comparison of two journeys in metric did not return -1,1 or 0 but " +
                                            duel);
                }
            }


            //if (comparison == int.MaxValue)
            // The new journey is on the pareto front and can be added
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