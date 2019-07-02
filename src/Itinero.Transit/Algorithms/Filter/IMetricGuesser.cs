using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;

namespace Itinero.Transit.Journey.Filter
{
    /// <summary>
    /// The IMetricGuesser tries to guess a minimal time to complete the journey.
    /// Using this minimal time, we could know that a certain halfway journey can never be optimal
    /// and that we can prune this from the pareto frontiers.
    ///
    /// The heuristics can range from quite simple to horribly complicated.
    /// The simplest heuristic is to have a look at the current scantime and use that as heuristic for earliest arrival time.
    /// More complicated heuristics can start with a dijkstra from/to the arrival/departure stop to calculate a
    /// least needed traveltime.
    /// 
    /// </summary>
    public interface IMetricGuesser<T> where T : IJourneyMetric<T>
    {
        /// <summary>
        /// Calculates a connection which is _very_ optimistic to get the intermediate journey to the destination.
        /// The constructed journey will be checked against already known optimal routes.
        /// If the constructed journey can not be optimal, the intermediate journey can be pruned from the algorithm.
        /// </summary>
        /// <returns></returns>
        IConnection LeastTheoreticalConnection(Journey<T> intermediate);

        /// <summary>
        /// Returns whether or not it is useful to check this pareto frontier.
        /// (e.g. if we just cleaned up the frontier and nothing actually changed, it is no use to retry cleaning).
        ///
        /// </summary>
        /// <param name="frontier"></param>
        /// <returns></returns>
        bool ShouldBeChecked(ProfiledParetoFrontier<T> frontier);
    }
}