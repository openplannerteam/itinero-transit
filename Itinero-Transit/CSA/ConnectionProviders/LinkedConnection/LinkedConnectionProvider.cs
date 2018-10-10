using System;
using Itinero_Transit.LinkedData;
using JsonLD.Core;
using Serilog;

namespace Itinero_Transit.CSA.ConnectionProviders
{
    /// <summary>
    /// A LinkedConnectionProvider-object corresponds with one data source,
    /// whom offers the public transport data in LinkedConnections-format.
    ///
    /// The ontology can be found here
    /// </summary>
    public class LinkedConnectionProvider : IConnectionsProvider
    {

        private readonly Uri _baseUri;
        public readonly Downloader Loader;
        private readonly JsonLdProcessor _processor;

        public LinkedConnectionProvider(Uri baseUri)
        {
            _baseUri = baseUri;
            Loader = new Downloader();
            _processor = new JsonLdProcessor(Loader, baseUri);
        }

        public IConnection GetConnection(Uri id)
        {
            throw new NotImplementedException();
        }

        public ITimeTable GetTimeTable(Uri id)
        {
            
            var tt = new LinkedTimeTable(id);
            tt.Download(_processor);
            return tt;

        }

        public Uri TimeTableIdFor(DateTime includedTime)
        {
            throw new NotImplementedException();
        }

        public IConnection CalculateInterConnection(IConnection @from, IConnection to)
        {
            throw new NotImplementedException();
        }
    }
}