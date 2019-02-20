using System;
using System.Threading;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.IO.LC;
using Itinero.Transit.IO.LC.IO.LC.Synchronization;

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
                new SynchronizedWindow(5, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(600)));

            Thread.Sleep(6000);
            syncer.Stop();

            NotNull(syncer.CurrentlyRunning);
            NotNull(syncer.CurrentlyRunning.ToString());
            
            
            return input;
        }
    }
}