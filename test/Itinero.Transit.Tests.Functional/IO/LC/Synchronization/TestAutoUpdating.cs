using System;
using System.Threading;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.IO.LC;
using Itinero.Transit.IO.LC.IO.LC.Synchronization;
using Itinero.Transit.Tests.Functional.Data;

namespace Itinero.Transit.Tests.Functional.IO.LC.Synchronization
{
    public class TestAutoUpdating : FunctionalTest<object, object>
    {
        protected override object Execute(object input)
        {
            var tdb = new TransitDb();

            var sncb = Belgium.AllLinks["sncb"];
            var (syncer, _) = tdb.UseLinkedConnections(
                sncb.connections,
                sncb.locations,
                new SynchronizedWindow(5, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(3600)));

            Thread.Sleep(5500);
            NotNull(syncer.CurrentlyRunning);
            NotNull(syncer.CurrentlyRunning.ToString());
            
            Thread.Sleep(1000);
            syncer.Stop();

            
            
            return input;
        }
    }
}