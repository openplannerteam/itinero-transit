using System;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using Serilog;

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


        public static void AsJson(string contents)
        {
            dynamic json = JObject.Parse(contents);
            Log.Information(json);
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

            var client = new WebClient();

            client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

            var data = client.OpenRead(uri);
            if (data == null)
            {
                throw new FileNotFoundException("Could not open " + uri);
            }

            var reader = new StreamReader(data);
            string s;
            try
            {
                s = reader.ReadToEnd();
            }
            finally
            {
                reader.Close();
            }

            return s;
        }
    }
}