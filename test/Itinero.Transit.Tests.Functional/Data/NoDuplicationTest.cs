using System;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC;
using Itinero.Transit.IO.LC.Synchronization;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class NoDuplicationTest : FunctionalTest<bool, bool>
    {
        private int countConnections(DateTime now, TransitDb tdb)
        {
            var latest = tdb.Latest;
            var count = 0;
            var enumerator = latest.ConnectionsDb.GetDepartureEnumerator();
            enumerator.MoveNext(now);
            while (enumerator.MoveNext())
            {
                count++;
            }

            return count;
        }

        protected override bool Execute(bool _)
        {
            var now = DateTime.Now;

            var tdb = new TransitDb();
            var dataset = tdb.UseLinkedConnections(Belgium.SNCB_Connections, Belgium.SNCB_Locations,
                DateTime.MaxValue, DateTime.MinValue);
            var updater = new TransitDbUpdater(tdb, dataset.UpdateTimeFrame);

            updater.UpdateTimeFrame(now, now.AddMinutes(10));
            var totalConnections = countConnections(now, updater.TransitDb);

            for (var i = 0; i < 10; i++)
            {
                updater.UpdateTimeFrame(now, now.AddMinutes(10));
                var newCount = countConnections(now, updater.TransitDb);
                if (newCount > totalConnections * 1.1)
                {
                    throw new ArgumentException("Duplicates are building in the database");
                }
            }

            return true;
        }
    }
}