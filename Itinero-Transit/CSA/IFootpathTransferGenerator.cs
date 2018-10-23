using System;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// The footpath-generator is the one responsible to create transfers where the traveller transfers from one
    /// platform to another, possibly making an intermodal transfer.
    ///
    /// This results in a 'continuous' transfer
    /// </summary>
    public interface IFootpathTransferGenerator
    {

        IConnection GenerateFootPaths(DateTime departureTime, Uri from, Uri to);
      
        /// <summary>
        /// Given two connections (e.g. within the same station; or to a bus station which is close by),
        /// calculates an object representing the transfer (e.g. walking from platform 2 to platform 5; or walking 250 meters)
        /// </summary>
        /// <param name="from">The connection that the newly calculated connection continues on</param>
        /// <param name="to">The connection that should be taken after the returned connection</param>
        /// <returns>A connection representing the transfer. Returns null if no transfer is possible (e.g. to little time)</returns>
        IConnection CalculateInterConnection(IConnection from, IConnection to);
    }
}