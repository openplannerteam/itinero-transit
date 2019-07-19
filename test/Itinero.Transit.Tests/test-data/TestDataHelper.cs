using System;
using System.IO;

namespace Itinero.Transit.Tests
{
    public static class TestDataHelper
    {
        /// <summary>
        /// Loads a string from an embedded resource stream.
        /// </summary>
        internal static byte[] LoadEmbeddedResource(string resource)
        {
            using (var stream = typeof(TestDataHelper).Assembly.GetManifestResourceStream(resource))
            {
                if (stream == null) throw new Exception($"Resource {resource} not found.");
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }
    }
}