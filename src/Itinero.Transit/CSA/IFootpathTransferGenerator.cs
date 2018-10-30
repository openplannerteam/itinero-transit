using System;
using Itinero_Transit.CSA.ConnectionProviders.LinkedConnection;

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

        IContinuousConnection GenerateFootPaths(DateTime departureTime, Location from, Location to);
        
    }
}