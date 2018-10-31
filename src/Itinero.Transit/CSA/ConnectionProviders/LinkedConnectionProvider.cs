using System;
using JsonLD.Core;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Itinero.Transit
{
    /// <inheritdoc />
    ///  <summary>
    ///  A LinkedConnectionProvider-object corresponds with one data source,
    ///  whom offers the public transport data in LinkedConnections-format.
    ///  The ontology can be found here
    ///  </summary>
    public class LinkedConnectionProvider : IConnectionsProvider
    {
        private readonly JsonLdProcessor _processor;

        private readonly string _searchTemplate;

        /// <summary>
        /// Creates a new Connections-provider, based on a 'hydra-search' field.
        /// The 'hydra-search' should already be expanded JSON-LD
        /// </summary>
        public LinkedConnectionProvider(JToken hydraSearch, Downloader loader = null)
        {
            _searchTemplate = hydraSearch.GetLDValue("http://www.w3.org/ns/hydra/core#template");
           
            Log.Information($"Search template is {_searchTemplate}");
            // TODO Softcode departure time argument
            var baseString = _searchTemplate.Replace("{?departureTime}", "");
            Log.Information($"Base string is {baseString}");
            var baseUri = new Uri(baseString);
            loader = loader ?? new Downloader();
            _processor = new JsonLdProcessor(loader, baseUri);
        }
        
        public LinkedConnectionProvider(Uri baseUri, string searchUri, Downloader loader = null)
        {
            _searchTemplate = searchUri;
            loader = loader ?? new Downloader();
            _processor = new JsonLdProcessor(loader, baseUri);
        }

        public ITimeTable GetTimeTable(Uri id)
        {
            var tt = new LinkedTimeTable(id);
            tt.Download(_processor);
            return tt;
        }

        public Uri TimeTableIdFor(DateTime time)
        {
            time = time.AddSeconds(-time.Second).AddMilliseconds(-time.Millisecond);
            var timeString = $"{time:yyyy-MM-ddTHH:mm:ss}.000Z";
            return new Uri(_searchTemplate.Replace("{?departureTime}", $"?departureTime={timeString}"));
        }
    }
}