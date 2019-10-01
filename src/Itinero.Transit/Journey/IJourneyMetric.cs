using Itinero.Transit.Data.Core;

namespace Itinero.Transit.Journey
{
    ///  <summary>
    ///  Every journey carries a 'journeyMetric'-object.
    ///  This is an object keeping track of metrics for each journey.
    ///  The user of the route planner chooses exactly which metrics are kept and is in control of the comparison.
    ///  This way, there is a lot of freedom on what to optimize on or to keep a pareto front in each stop.
    ///  Note: there is one metric that is tracked by the journey itself: the arrival time.
    ///  This is because the arrival time is needed in the CSA algorithm.
    ///  </summary>
    public interface IJourneyMetric<T>
        where T : IJourneyMetric<T>
    {
        /// <summary>
        /// Gives an object containing metrics for a journey which hasn't begun yet.
        /// A good candidate to reuse is an empty factory object 
        /// 
        /// 'Even the longest journey begins with the zeroth step'
        /// </summary>
        T Zero();

      
        /// <summary>
        /// A new metrics object that represents the new metrics when this connection is taken.
        /// nextPiece.PreviousLink should not be null
        /// </summary>
        T Add(Journey<T> previousJourney, StopId currentLocation, ulong currentTime, TripId currentTripId, bool currentIsSpecial);

    }
}