using System;
using Itinero.Transit.IO.LC.Utils;
using JsonLD.Core;

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


        public ConnectionProvider(Uri baseUri, string searchUri)
        {
            _searchTemplate = searchUri;
            _processor = new JsonLdProcessor(_loader, baseUri);
        }

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