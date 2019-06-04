using System.IO;
using System.Threading;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Synchronization;
using Itinero.Transit.IO.LC;

namespace Itinero.Transit.Tests.Functional.IO.LC.Synchronization
{
    public class TestWriteToDisk : FunctionalTest<TransitDb, (TransitDb db, string name) >
    {
        protected override TransitDb Execute((TransitDb db, string name) input)
        {
            var path = $"test-write-to-disk-{input.name}.transitdb";
            var syncer = input.db.AddSyncPolicy(new WriteToDisk(1, path));

            Thread.Sleep(1200);
            syncer.Stop();

            // Wait till the other thread is done writing
            Thread.Sleep(10000);


            using (var stream = File.OpenRead(path))
            {
                return ReadTransitDbTest.Default.Run(stream);
            }
        }
    }
}