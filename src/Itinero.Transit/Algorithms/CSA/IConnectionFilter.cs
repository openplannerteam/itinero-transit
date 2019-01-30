using Itinero.Transit.Data;

namespace Itinero.Transit.Algorithms.CSA
{
    /// <summary>
    /// A connection filter helps to optimize PCS by saying if a connection can be taken or not.
    ///
    /// For example, a previous Earliest Arrival Scan can determine that,
    /// if the traveller is leaving at 10:00 in Bruges, it is impossible for them to be in Brussels at 10:10.
    /// The filter will indicate that the trip can not be taken and throw it out
    /// 
    /// </summary>
    internal interface IConnectionFilter
    {

        /// <summary>
        /// Is it useful to check this connection in PCS?
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        bool CanBeTaken(IConnection c);
        
        /// <summary>
        /// Didn't we fuck up? E.G. we want to check connections which fall before the earliest connection scan
        /// Throws an exception if non-applicable
        /// </summary>
        /// <param name="depTime"></param>
        /// <param name="arrTime"></param>
        /// <returns></returns>
        void CheckWindow(ulong depTime, ulong arrTime);

    }
}