using System;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Synchronization;
using Itinero.Transit.IO.LC;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class MultipleLoadTest : FunctionalTest<uint, uint>
    {
        protected override uint Execute(uint _)
        {
            var sncb = Belgium.Sncb();

            void UpdateTimeFrame(TransitDb.TransitDbWriter w, DateTime start, DateTime end)
            {
                sncb.AddAllConnectionsTo(w, start, end);
            }

            var db = new TransitDb();
            var dbUpdater = new TransitDbUpdater(db, UpdateTimeFrame);

            var writer = db.GetWriter();
            sncb.AddAllLocationsTo(writer);
            writer.Close();

            var hours = 24;

            dbUpdater.UpdateTimeFrame(DateTime.Today.ToUniversalTime(),
                DateTime.Today.AddHours(hours).ToUniversalTime());
            Test(db);

            dbUpdater.UpdateTimeFrame(DateTime.Today.AddDays(1).ToUniversalTime(),
                DateTime.Today.AddDays(1).AddHours(hours).ToUniversalTime());
            Test(db);

            dbUpdater.UpdateTimeFrame(DateTime.Today.AddHours(-hours).ToUniversalTime(),
                DateTime.Today.AddHours(0).ToUniversalTime());
            Test(db);

            return 1;
        }

        private void Test(TransitDb db)
        {
            var conns = db.Latest.ConnectionsDb;

            var enumerator = conns.GetDepartureEnumerator();
            var count = 0;

            enumerator.MoveTo(DateTime.Today.AddHours(10).ToUniversalTime().ToUnixTime());
            var endTime = DateTime.Today.AddHours(11).ToUniversalTime().ToUnixTime();
            while (enumerator.HasNext() && enumerator.CurrentDateTime < endTime)
            {
                count++;
            }

            True(count > 0);
            new TripHeadsignTest().Run(db);
        }
    }
}