using System;

namespace Itinero_Transit.CSA
{
    /// <inheritdoc />
    ///  <summary>
    ///  Every journey carries a 'journeyStats'-object.
    ///  This is an object keeping track of statistics for each journey.
    ///  The user of the route planner chooses exactly which statistics are kept and is in control of the comparison.
    ///  This way, there is a lot of freedom on what to optimize on or to keep a pareto front in each stop.
    ///  Note: there is one statistic that is tracked by the journey itself: the arrival time.
    ///  This is because the arrival time is needed in the CSA algorithm.
    ///  </summary>
    public interface IJourneyStats
    {
        /// <summary>
        /// Create statistics for a single connection, used to start the journey statistics.
        /// </summary>
        /// <returns></returns>
        IJourneyStats InitialStats(Connection c);

        /// <summary>
        /// A new statistics object that represents the new statistics when this connection is taken.
        /// nextPiece.PreviousLink should not be null
        /// </summary>
        IJourneyStats Add(Journey journey);
        

    }
}