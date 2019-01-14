using System.IO;
using Itinero.Transit.Data;

namespace Itinero.Transit.Tests.Functional.IO.LC
{
    /// <summary>
    /// Tests the writing a transit db to a stream.
    /// </summary>
    public class WriteTransitDbTest : FunctionalTest<Stream, TransitDb>
    {
        /// <summary>
        /// Gets the default test.
        /// </summary>
        public static WriteTransitDbTest Default => new WriteTransitDbTest();
        
        protected override Stream Execute(TransitDb input)
        {
            var stream = new MemoryStream();

            input.Latest.WriteTo(stream);

            return stream;
        }
    }
}