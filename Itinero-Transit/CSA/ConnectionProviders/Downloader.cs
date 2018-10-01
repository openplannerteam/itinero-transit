using System;
using System.IO;
using System.Net.Http;
using CacheCow.Client;
using CacheCow.Client.Headers;
using Newtonsoft.Json.Linq;

namespace Itinero_Transit.LinkedData
{
    /// <summary>
    /// Utilities to help downloading, caching and testing (e.g. to inject a fixed string while testing)
    /// </summary>
    public static class Downloader
    {
        /// <summary>
        /// This string can be set during tests, in which this string will _always_ be given as "downloaded" string
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public static string AlwaysReturn = null;

        public static int DownloadCounter;
        public static int CacheHits;
        public static double TimeDownloading;

        private static readonly HttpClient Client = CreateClient();

        private static HttpClient CreateClient()
        {
            var store = new FileStore("cache");
            store.Remove("y22xKWDh3wbEohramNuigPEBJtk=");
            var client = store.CreateClient();
            client.DefaultRequestHeaders.Add("user-agent", "Itinero-Transit-dev/0.0.1");
            client.DefaultRequestHeaders.Add("accept", "application/ld+json");
            return client;
        }

        public static string Download(Uri uri)
        {
            return DownloadRaw(uri);
        }

        public static JObject DownloadJson(Uri uri)
        {
            return AsJson(DownloadRaw(uri));
        }


        public static JObject AsJson(string contents)
        {
            var json = JObject.Parse(contents);
            return json;
        }

        /// <summary>
        /// Actually download the contents.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        private static string DownloadRaw(Uri uri)
        {
            if (AlwaysReturn != null)
            {
                // Used for testing
                return AlwaysReturn;
            }

            DownloadCounter++;
            var start = DateTime.Now;

            var response = Client.GetAsync(uri).ConfigureAwait(false).GetAwaiter().GetResult();
            if (response == null)
            {
                throw new FileNotFoundException("Could not open " + uri);
            }

            var data = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            var end = DateTime.Now;

            if (response.Headers.GetCacheCowHeader().ToString().Contains("did-not-exist=false"))
            {
                CacheHits++;
            }

            TimeDownloading += (end - start).TotalMilliseconds;
            return data;
        }

        // ReSharper disable once UnusedMember.Global
        public static void ResetCounters()
        {
            TimeDownloading = 0;
            DownloadCounter = 0;
            CacheHits = 0;
        }
    }
}