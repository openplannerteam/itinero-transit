namespace Itinero_Transit.CSA
{
    /// <summary>
    /// The interface that objects comparing statistics fullfill.
    /// The implementation is free to compare one or more dimensions.
    /// In the case that multiple dimensions are used, 
    /// </summary>
    public interface IStatsComparator<in T>
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
        int ADominatesB(T a, T b);
    }
}