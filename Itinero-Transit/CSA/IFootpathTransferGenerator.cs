using System;
using System.Collections.Generic;

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

    }
}