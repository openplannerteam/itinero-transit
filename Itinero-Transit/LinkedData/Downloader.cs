using System;
using System.IO;
using CacheCow.Client;
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

            var client = ClientExtensions.CreateClient();
            

        //    client..Headers.Add("user-agent", "Itinero-Transit-v0.0.1");
       //     client.Headers.Add("accept", "application/ld+json");

            var data = client.GetAsync(uri).ConfigureAwait(false).GetAwaiter().GetResult();
            if (data == null)
            {
                throw new FileNotFoundException("Could not open " + uri);
            }

            return data.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}