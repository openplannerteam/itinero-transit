using System;

namespace Itinero.Transit.IO.LC.CSA
{
    /// <summary>
    /// The footpath-generator is the one responsible to create transfers where the traveller transfers from one
    /// platform to another, possibly making an intermodal transfer.
    ///
    /// This results in a 'continuous' transfer
    /// </summary>
    internal interface IFootpathTransferGenerator
    {

        //IContinuousConnection GenerateFootPaths(DateTime departureTime, Location from, Location to);
        
    }
}