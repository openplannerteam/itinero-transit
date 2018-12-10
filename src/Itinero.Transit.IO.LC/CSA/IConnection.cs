using System;

namespace Itinero.Transit.IO.LC.CSA
{
    /// <inheritdoc />
    /// <summary>
    /// Represents an abstract connection from one point to another using a single transportation mode (e.g. train, walking, cycling, ...)
    /// Note that a single transfer from one train to another is modelled as a connection too
    /// </summary>
    public interface IConnection : IJourneyPart
    {
        /// <summary>
        /// The identifier of the operator
        /// </summary>
        /// <returns></returns>
        Uri Operator();

        /// <summary>
        /// The identifier of this single connection (e.g. between Brussels-North and Brussels-Central)
        /// </summary>
        /// <returns></returns>
        Uri Id();
        
        /// <summary>
        /// The identifier of the longer trip (e.g. Oostende-Eupen, leaving at 10:10), which can contain multiple single connections.
        /// Will be null for some connections, e.g. when walking.
        /// </summary>
        /// <returns></returns>
        Uri Trip();

        /// <summary>
        /// The identifier of the fixed route that trains ride multiple times per day/week (e.g. the route between Oostende-Eupen) without specifying the exact moment in time
        /// </summary>
        /// <returns></returns>
        Uri Route();
    }
}