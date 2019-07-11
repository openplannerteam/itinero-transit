using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CacheCow.Client;
using CacheCow.Client.Headers;
using Itinero.Transit.Logging;
using JsonLD.Core;
using Newtonsoft.Json.Linq;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]

namespace Itinero.Transit.IO.LC.Data
{
    public class Downloader : IDocumentLoader
    {
        /// <summary>
        /// This string can be set during tests, in which this string will _always_ be given as "downloaded" string
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public string AlwaysReturn = null;


        private readonly HttpClient _client;

        private readonly Dictionary<string, bool> _isCached = new Dictionary<string, bool>();

        public Downloader()
        {
            _client = ClientExtensions.CreateClient();
            _client.DefaultRequestHeaders.Add("user-agent",
                "Itinero-Transit-dev/0.0.2 (anyways.eu; pieter@anyways.eu)");
            _client.DefaultRequestHeaders.Add("accept", "application/ld+json");
            _client.Timeout = TimeSpan.FromMilliseconds(5000);
        }


        public JToken LoadDocument(Uri uri)
        {
            return JObject.Parse(DownloadRaw(uri).ConfigureAwait(false).GetAwaiter().GetResult());
        }


        /// <summary>
        /// Actually download the contents.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        private async Task<string> DownloadRaw(Uri uri)
        {
            if (AlwaysReturn != null)
            {
                // Used for testing
                return AlwaysReturn;
            }

            if (!string.IsNullOrEmpty(uri.Fragment))
            {
                var u = uri.ToString();
                uri = new Uri(u.Substring(0, u.Length - uri.Fragment.Length));
            }

            var start = DateTime.Now;

            try
            {
                var response = await _client.GetAsync(uri).ConfigureAwait(false);
                if (response == null || !response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException("Could not open " + uri);
                }

                var data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var cacheCowHeader = response.Headers.GetCacheCowHeader();
                var cached = !cacheCowHeader.ToString().Contains("did-not-exist=true");
                _isCached[uri.ToString()] = cached;

                var end = DateTime.Now;


                var timeNeeded = (end - start).TotalMilliseconds / 1000;
                Log.Verbose(
                    $"Downloading {uri} completed in {timeNeeded}s, got {data.Length} bytes");
                return data;
            }
            catch (Exception e)
            {
                Log.Error($"Loading {uri} failed");
                throw new ArgumentException($"Could not download {uri}", e);
            }
        }

        public bool IsCached(string url)
        {
            return _isCached.ContainsKey(url) && _isCached[url];
        }
    }
}