using System.IO;
using System.Threading;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Serialization;
using Itinero.Transit.Data.Synchronization;
using Itinero.Transit.IO.LC;
using Itinero.Transit.Tests.Functional.Utils;

namespace Itinero.Transit.Tests.Functional.IO.LC.Synchronization
{
    public class TestWriteToDisk : FunctionalTestWithInput<TransitDb>
    {
        
        public override string Name => "Test Writing to disk";

        protected override void Execute()
        {
            var path = $"test-write-to-disk-nmbs.transitdb";
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var syncer = Input.AddSyncPolicy(new WriteToDisk(1, path));
            syncer.Start();
            Thread.Sleep(1200);
            syncer.Stop();

            // Wait till the other thread is done writing
            Thread.Sleep(10000);
            True(File.Exists(path));

            // can we read this stuff again?
            var read = new TransitDb(0);
            var writer = read.GetWriter();
            writer.ReadFrom(path);
            read.CloseWriter();
            NotNull(read);

            File.Delete(path);
        }
    }
}