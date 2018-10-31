namespace Itinero.Transit
{
    /// <summary>
    /// The interface that objects comparing statistics fullfill.
    /// The implementation is free to compare one or more dimensions.
    /// In the case that multiple dimensions are used, certain algorithms will return a pareto-frontier or profile-frontier.
    ///
    /// WHen running in CSP, you'll want to use a profile-comparsion; afterwards you can prune the found journeys with a real
    /// pareto-frontier.
    ///
    /// Note that this will be very user-specific
    /// 
    /// </summary>
    public abstract class StatsComparator<T>
        where T : IJourneyStats<T>
    {
        /// <summary>
        /// Returns (-1) if A is smaller (and thus more optimized),
        /// Return 1 if B is smaller (and thus more optimized)
        /// Return 0 if they are equally optimal
        /// Return Int.MAX_VALUE if they can not be compared and are both part of the pareto frontier
        /// /// </summary>
        /// <param name="a">The first statistics to compare</param>
        /// <param name="b">The second statistics to compare</param>
        /// <returns></returns>
        public abstract int ADominatesB(T a, T b);


        public int ADominatesB(Journey<T> a, Journey<T> b)
        {
            return ADominatesB(a.Stats, b.Stats);
        }
        
    }
}