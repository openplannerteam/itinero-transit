using System;
using System.IO;
using System.Net.Http;
using System.Web;
using Itinero.Logging;

namespace Itinero.Transit.Tests.Functional.Staging
{
    internal class TilesDownloadHelper
    {
        /// <summary>
        /// Gets a stream for the content at the given url.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <returns>An open stream for the content at the given url.</returns>
        public static Stream Download(string url)
        {
            var fileName = HttpUtility.UrlEncode(url) + ".tile";
            fileName = Path.Combine(".", "cache", fileName);

            if (!File.Exists(fileName))
            {
                try
                {
                    Console.WriteLine($"Downloading: {url}");
                    var client = new HttpClient();
                    var response = client.GetAsync(url);
                    var responseResult = response.GetAwaiter().GetResult();
                    using (var fileStream = File.Open(fileName, FileMode.Create))
                    {
                        if (responseResult.IsSuccessStatusCode)
                        {
                            using (var stream = responseResult.Content.ReadAsStreamAsync().GetAwaiter()
                                .GetResult())
                            {
                                stream.CopyTo(fileStream);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Itinero.Logging.Logger.Log(nameof(TilesDownloadHelper), TraceEventType.Warning,
                        $"Failed to download from {url}: {ex}.");
                    return null;
                }
            }

            var cachedFileStream = File.OpenRead(fileName);
            if (cachedFileStream.Length == 0)
            {
                cachedFileStream.Dispose();
                return null;
            }

            return cachedFileStream;
        }
    }
}