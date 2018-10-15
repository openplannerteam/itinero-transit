using System;
using Itinero_Transit.LinkedData;
using JsonLD.Core;
using Newtonsoft.Json.Linq;
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
        private readonly Downloader _loader;
        private readonly JsonLdProcessor _processor;

        private readonly string _searchTemplate;


        /// <summary>
        /// Creates a new Connections-provider, based on a 'hydra-search' field.
        /// The 'hydra-search' should already be expanded JSON-LD
        /// </summary>
        /// <param name="hydraSearch"></param>
        public LinkedConnectionProvider(JObject hydraSearch)
        {
            Log.Information(hydraSearch.ToString());
            _searchTemplate = hydraSearch["http://www.w3.org/ns/hydra/core#template"][0]["@value"].ToString();

            // TODO SOftcode departure time argument
            var baseString = _searchTemplate.Replace("{?departureTime}", "");
            Log.Information(baseString);
            var baseUri = new Uri(baseString);
            _loader = new Downloader();
            _processor = new JsonLdProcessor(_loader, baseUri);
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

        public ITimeTable GetTimeTable(DateTime time)
        {
            return GetTimeTable(TimeTableIdFor(time));
        }

        public Uri TimeTableIdFor(DateTime time)
        {
            time = time.AddSeconds(-time.Second).AddMilliseconds(-time.Millisecond);
            var timeString = $"{time:yyyy-MM-ddTHH:mm:ss}.000Z";
            return new Uri(_searchTemplate.Replace("{?departureTime}", $"?{timeString}"));
        }

        public IConnection CalculateInterConnection(IConnection @from, IConnection to)
        {
            throw new NotImplementedException();
        }
    }
}