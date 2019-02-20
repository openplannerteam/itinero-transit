using System.IO;
using Itinero.Transit.Data;
using Itinero.Transit.Tests.Functional.IO.LC;

namespace Itinero.Transit.Tests.Functional.Data
{
    /// <summary>
    /// Simply write the transitDB and reads it from a in-memory stream
    /// </summary>
    public class TestReadWrite : FunctionalTest<bool, TransitDb>
    {
        protected override bool Execute(TransitDb db)
        {
            using (var stream = WriteTransitDbTest.Default.Run(db))
            {
                stream.Seek(0, SeekOrigin.Begin);
                db = ReadTransitDbTest.Default.Run(stream);
            }

            return true;
        }
    }
}