using System.Collections.Generic;
using Serilog;

namespace Itinero_Transit.CSA
{
    public class ParetoFrontier<T>
        where T : IJourneyStats<T>

    {
        private readonly StatsComparator<T> _comparator;

        public ParetoFrontier(StatsComparator<T> comparator)
        {
            _comparator = comparator;
        }

        /// <summary>
        /// Given a list of journeys, returns a new collection only returning journeys on the pareto frontier
        /// </summary>
        /// <param name="???"></param>
        /// <param name="comparator"></param>
        /// <returns></returns>
        public HashSet<Journey<T>> ParetoFront(IEnumerable<Journey<T>> journeys)
        {
            var frontier = new HashSet<Journey<T>>();
            var toRemove = new HashSet<Journey<T>>();
            Log.Information("Doing postfiltering");
            foreach (var considered in journeys)
            {
                Log.Information("Considering" + considered);
                toRemove.Clear();
                var defeated = false;
                foreach (var guard in frontier)
                {
                    var comparison = _comparator.ADominatesB(guard, considered);
                    if (comparison < 0)
                    {
                        // The new journey didn't make the cut
                        defeated = true;
                        Log.Information("Defeated by " + guard.Stats);
                        continue;
                    }

                    if (comparison == 1)
                    {
                        // The new journey defeated the guard
                        toRemove.Add(guard);
                        Log.Information("Guard defeated! He was: " + guard.Stats);
                    }

                    //if (comparison == int.MaxValue)
                    // Both are on the pareto front

                    //if (comparison == 0)
                    // Both are equally good
                    // As both might leave at different hours, we add the new journey as well
                }


                if (!defeated)
                {
                    Log.Information($"Welcoming {considered.Stats} in the frontier");
                    frontier.Add(considered);
                }


                foreach (var defeatedGuard in toRemove)
                {
                    frontier.Remove(defeatedGuard);
                }
            }

            return frontier;
        }
    }
}