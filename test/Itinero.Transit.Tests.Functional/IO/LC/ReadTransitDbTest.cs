using System.IO;
using Itinero.Transit.Data;

namespace Itinero.Transit.Tests.Functional.IO.LC
{
    /// <summary>
    /// Tests the writing a transit db to disk.
    /// </summary>
    public class ReadTransitDbTest : FunctionalTest<TransitDb, Stream>
    {
        /// <summary>
        /// Gets the default update connections test.
        /// </summary>
        public static ReadTransitDbTest Default => new ReadTransitDbTest();
        
        protected override TransitDb Execute(Stream input)
        {
            return TransitDb.ReadFrom(input, 0);
        }
    }
}