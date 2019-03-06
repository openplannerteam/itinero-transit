using System.IO;
using System.Threading;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC;
using Itinero.Transit.IO.LC.Synchronization;

namespace Itinero.Transit.Tests.Functional.IO.LC.Synchronization
{
    public class TestWriteToDisk : FunctionalTest<TransitDb, TransitDb>
    {
        protected override TransitDb Execute(TransitDb input)
        {
            var path = "test-write-to-disk.transitdb";
            var syncer = input.AddSyncPolicy(new WriteToDisk(1, path));

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