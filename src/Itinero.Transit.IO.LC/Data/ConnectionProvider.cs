using System;
using Itinero.Transit.Logging;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

namespace Itinero.Transit.IO.LC.Data
{
    ///  <summary>
    ///  A LinkedConnectionProvider-object corresponds with one data source,
    ///  whom offers the public transport data in LinkedConnections-format.
    ///  The ontology can be found here
    ///  </summary>
    public class ConnectionProvider
    {
        private readonly JsonLdProcessor _processor;

        private readonly string _searchTemplate;

        private readonly Downloader _loader = new Downloader();


        /// <summary>
        /// Creates a new Connections-provider, based on a 'hydra-search' field.
        /// The 'hydra-search' should already be expanded JSON-LD
        /// </summary>
        public ConnectionProvider(JToken hydraSearch)
        {
            _searchTemplate = hydraSearch.GetLDValue("http://www.w3.org/ns/hydra/core#template");

            Log.Information($"Search template is {_searchTemplate}");
            // TODO Softcode departure time argument
            var baseString = _searchTemplate.Replace("{?departureTime}", "");
            Log.Information($"Base string is {baseString}");
            var baseUri = new Uri(baseString);
            _processor = new JsonLdProcessor(_loader, baseUri);
        }

        public ConnectionProvider(Uri baseUri, string searchUri)
        {
            _searchTemplate = searchUri;
            _processor = new JsonLdProcessor(_loader, baseUri);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public (TimeTable, bool hasChanged) GetTimeTable(Uri id)
        {
            var tt = new TimeTable(id);
            tt.Download(_processor);
            var wasCached = _loader.IsCached(id.ToString());
            return (tt, !wasCached);
        }

        public Uri TimeTableIdFor(DateTime time)
        {
            time = time.AddSeconds(-time.Second).AddMilliseconds(-time.Millisecond);
            var timeString = $"{time:yyyy-MM-ddTHH:mm:ss}.000Z";
            return new Uri(_searchTemplate.Replace("{?departureTime}", $"?departureTime={timeString}"));
        }
    }
}