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

        public static int DownloadCounter = 0;
        public static int CacheHits = 0;
        public static double TimeDownloading = 0;

        private static readonly HttpClient client = new FileStore("cache").CreateClient();


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


            //    client..Headers.Add("user-agent", "Itinero-Transit-v0.0.1");
            //     client.Headers.Add("accept", "application/ld+json");

            var response = client.GetAsync(uri).ConfigureAwait(false).GetAwaiter().GetResult();
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

        public static void ResetCounters()
        {
            TimeDownloading = 0;
            DownloadCounter = 0;
            CacheHits = 0;
        }
    }
}