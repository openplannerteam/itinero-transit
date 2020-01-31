using System.IO;
using System.Net;

namespace Itinero.Transit.Tests.Functional.Staging
{
    internal static class Download
    {
        public static void Get(string url, string fileName)
        {
            if (!File.Exists(fileName))
            {
                var client = new WebClient();
                client.DownloadFile(url,
                    fileName);
            }
        }
    }
}