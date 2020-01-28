using System.IO;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Serialization;
using Itinero.Transit.Tests.Functional.Utils;

namespace Itinero.Transit.Tests.Functional.Data
{
    /// <summary>
    /// Simply write the transitDB and reads it from a in-memory stream
    /// </summary>
    public class ReadWriteTest : FunctionalTestWithInput<(TransitDb, uint expectedNumberOfConnections)>
    {  public override string Name => "Read Write Test";

        protected override void Execute()
        {
            using (var stream = new MemoryStream())
            {
                Input.Item1.Latest.WriteTo(stream);
                stream.Seek(0, SeekOrigin.Begin);
                var newTdb = new TransitDb(0);
                newTdb.GetWriter().ReadFrom(stream);
                newTdb.CloseWriter();
                True(Equals(Input.Item1.Latest.Connections.LatestDate, newTdb.Latest.Connections.LatestDate));
                new ConnectionsDbDepartureEnumeratorTest().Run((newTdb, Input.expectedNumberOfConnections));

            }
        }
    }
}