using System;

namespace Itinero_Transit.CSA
{
    /// <summary>
    /// The footpath-generator is the one responsible to create transfers where the traveller transfers from one
    /// platform to another, possibly making an intermodal transfer.
    ///
    /// This results in a 'continuous' transfer
    /// Note that, for optimization, we can request paths from one point to multiple other points
    /// </summary>
    public interface IFootpathTransferGenerator
    {

        List<IConnection> GenerateFootPaths(Uri from, List<Uri> to);

    }
}