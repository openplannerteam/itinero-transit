using System;
using System.Runtime.ConstrainedExecution;
using Itinero;

namespace Itinero_Transit.CSA.Connections
{
    public class OsmFootpathGenerator : IFootpathTransferGenerator
    {

        private readonly ILocationProvider _locationDecoder;
        private readonly RouterDb _routerDb = new RouterDb();
        

        public OsmFootpathGenerator(ILocationProvider locationDecoder, string routerdbPath)
        {
            _locationDecoder = locationDecoder;
            
        }

        public List<IConnection> GenerateFootPaths(Uri @from, List<Uri> to)
        {
            throw new NotImplementedException();
        }
    }
}