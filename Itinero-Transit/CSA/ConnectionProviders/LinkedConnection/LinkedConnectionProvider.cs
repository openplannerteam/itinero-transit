using System;
using Itinero_Transit.LinkedData;
using JsonLD.Core;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Itinero_Transit.CSA.ConnectionProviders
{
    /// <inheritdoc />
    ///  <summary>
    ///  A LinkedConnectionProvider-object corresponds with one data source,
    ///  whom offers the public transport data in LinkedConnections-format.
    ///  The ontology can be found here
    ///  </summary>
    public class LinkedConnectionProvider : IConnectionsProvider
    {
        // ReSharper disable once NotAccessedField.Global
        public readonly Downloader Downloader;
        private readonly JsonLdProcessor _processor;
        private readonly ILocationProvider _locationProvider;
        
        private readonly string _searchTemplate;

        private const double TransferSecondsNeeded = 60 * 3;


        /// <summary>
        /// Creates a new Connections-provider, based on a 'hydra-search' field.
        /// The 'hydra-search' should already be expanded JSON-LD
        /// </summary>
        public LinkedConnectionProvider(JObject hydraSearch, ILocationProvider locationMetadata, Downloader loader = null)
        {
            _searchTemplate = hydraSearch.GetLDValue("http://www.w3.org/ns/hydra/core#template");
            if (_searchTemplate.StartsWith("https://"))
            {
                _searchTemplate = "http" + _searchTemplate.Substring(5);
            }
            Log.Information($"Search template is {_searchTemplate}");
            // TODO Softcode departure time argument
            var baseString = _searchTemplate.Replace("{?departureTime}", "");
            Log.Information($"Basestring is {baseString}");
            var baseUri = new Uri(baseString);
            Downloader = loader ?? new Downloader();
            _processor = new JsonLdProcessor(Downloader, baseUri);
            _locationProvider = locationMetadata;
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

        /// <inheritdoc />
        ///  <summary>
        ///  Create a transfer through a SNCB-station from one train to another.
        ///  For now, a simple 'transfer-connection' is created. In the future, more advanced connections can be used
        ///  (e.g. with instructions through the station...)
        ///  Returns null if the transfer can't be made (transfertime is not enough)
        ///  Returns connection 'to' if the connection is on the same trip
        ///  </summary>
        ///  <param name="from"></param>
        ///  <param name="to"></param>
        ///  <returns></returns>
        public IConnection CalculateInterConnection(IConnection @from, IConnection to)
        {
            
            // TODO generalize this to a transferpolicy
            if ((to.DepartureTime() - from.ArrivalTime()).TotalSeconds < TransferSecondsNeeded)
            {
                // To little time to make the transfer
                return null;
            }

            return new InternalTransfer(to.DepartureLocation(), to.Operator(), from.ArrivalTime(),
                from.ArrivalTime().AddSeconds(TransferSecondsNeeded));
        }

        public ILocationProvider LocationProvider()
        {
            return _locationProvider;
        }
        
    }
}