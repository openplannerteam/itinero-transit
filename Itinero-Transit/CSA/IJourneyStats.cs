using System;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// Every journey carries a 'journeyStats'-object.
    /// This is an object keeping track of statistics for each journey.
    /// 
    /// The user of the route planner chooses exactly which statistics are kept and is in control of the comparison.
    /// This way, there is a lot of freedom on what to optimize on or to keep a pareto front in each stop.
    ///
    /// Note: there is one statistic that is tracked by the journey itself: the arrival time.
    /// This is because the arrival time is needed in the CSA algorithm
    /// </summary>
    public interface IJourneyStats : IComparable
    {
        /// <summary>
        /// Create an empty placeholder of your statistics, representing the beginning of the journey.
        /// </summary>
        /// <returns></returns>
        IJourneyStats EmptyStats();

        /// <summary>
        /// A new statistics object that represents the new statistics when this connection is taken
        /// </summary>
        IJourneyStats Add(Journey nextPiece);
        
        /// <summary>
        /// Returns True inf one stat dominates the other in a Pareto-sense (or they are the same), False if both should be kept in the Pareto-Front
        /// </summary>
        /// <param name="stats"></param>
        /// <returns></returns>
        bool IsComparableTo(IJourneyStats stats);
    }
}