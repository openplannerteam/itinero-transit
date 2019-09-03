using System.IO;
using Itinero.Transit.Data;
using Itinero.Transit.Tests.Functional.Utils;

namespace Itinero.Transit.Tests.Functional.Data
{
    /// <summary>
    /// Simply write the transitDB and reads it from a in-memory stream
    /// </summary>
    public class ReadWriteTest : FunctionalTestWithInput<(TransitDb, uint expectedNumberOfConnections)>
    {
        protected override void Execute()
        {
            using (var stream = new MemoryStream())
            {
                Input.Item1.Latest.WriteTo(stream);
                stream.Seek(0, SeekOrigin.Begin);
                var newTdb = TransitDb.ReadFrom(stream, 0);
                True(Equals(Input.Item1.Latest.ConnectionsDb.LatestDate, newTdb.Latest.ConnectionsDb.LatestDate));
                new ConnectionsDbDepartureEnumeratorTest().Run((newTdb, Input.expectedNumberOfConnections));

            }
        }
    }
}