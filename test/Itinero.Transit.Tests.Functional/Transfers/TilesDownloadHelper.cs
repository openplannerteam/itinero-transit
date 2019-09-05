using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Web;
using Serilog;

namespace Itinero.Transit.Tests.Functional.Transfers
{
    /// <summary>
    /// Copies the http-response to file.
    /// Adds a checksum to detect corruptions
    /// </summary>
    // TODO: remove this later, the cache should be the router db only.
    internal class TilesDownloadHelper
    {
        private readonly string _cachingDir;

        public TilesDownloadHelper(string cachingDir)
        {
            if (!Directory.Exists(cachingDir))
            {
                Directory.CreateDirectory(cachingDir);
            }

            Log.Information($"OSM-routable-tiles are cached in {cachingDir}");

            _cachingDir = cachingDir;
        }

        /// <summary>
        /// Gets a stream for the content at the given url.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <returns>An open stream for the content at the given url.</returns>
        public Stream Download(string url)
        {
            var fileName = HttpUtility.UrlEncode(url) + ".tile";
            fileName = Path.Combine(_cachingDir, fileName);

            if (!File.Exists(fileName + ".hash") && File.Exists(fileName))
            {
                Log.Information("Hash of the file not found - probably downloading failed earlier on");
                File.Delete(fileName);
            }

            if (File.Exists(fileName + ".hash") && File.Exists(fileName))
            {
                var readHash = File.ReadAllText(fileName + ".hash");
                var calcHash = HashFor(fileName);
                if (!readHash.Equals(calcHash))
                {
                    File.Delete(fileName);
                    File.Delete(fileName + ".hash");
                    Log.Information("Removed corrupt tile " + fileName);
                }
            }


            if (!File.Exists(fileName))
            {
                Log.Information($"Downloading {url} as {fileName} wasn't found");
                try
                {
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

                    File.WriteAllText(fileName + ".hash", HashFor(fileName));
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, $"Failed to download from {url}: {ex}.");
                    return null;
                }
            }


            var cachedFileStream = File.OpenRead(fileName);
            return cachedFileStream;
        }

        public uint CachedTilesCount()
        {
            var d = new DirectoryInfo(_cachingDir);
            uint count = 0;
            foreach (var _ in d.GetFiles("*.tile"))
            {
                count++;
            }

            return count;
        }

        private string HashFor(string fileName)
        {
            using (var fileCheckStream = File.OpenRead(fileName))
            {
                return
                    BitConverter.ToString(SHA1.Create().ComputeHash(fileCheckStream));
            }
        }
    }
}