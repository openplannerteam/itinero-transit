using System;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC;
using Itinero.Transit.IO.LC.CSA;
using Xunit;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class MultipleLoadTest : FunctionalTest<uint, uint>
    {
        protected override uint Execute(uint input)
        {
            var sncb = Belgium.Sncb();

            void UpdateTimeFrame(TransitDb.TransitDbWriter w, DateTime start, DateTime end)
            {
                sncb.AddAllConnectionsTo(w, start, end, Serilog.Log.Warning);
            }

            var db = new TransitDb(UpdateTimeFrame);

            var writer = db.GetWriter();
            sncb.AddAllLocationsTo(writer, Serilog.Log.Warning, null);
            writer.Close();

            var hours = 24;

            db.UpdateTimeFrame(DateTime.Today, DateTime.Today.AddHours(hours));
            Test(db);

            db.UpdateTimeFrame(DateTime.Today.AddDays(1), DateTime.Today.AddDays(1).AddHours(hours));
            Test(db);

            db.UpdateTimeFrame(DateTime.Today.AddHours(-hours), DateTime.Today.AddHours(0));
            Test(db);

            return 1;
        }

        private void Test(TransitDb db)
        {
            var conns = db.Latest.ConnectionsDb;

            var enumerator = conns.GetDepartureEnumerator();
            var count = 0;

            enumerator.MoveNext(DateTime.Today.AddHours(10));
            var endTime = DateTime.Today.AddHours(11).ToUnixTime();
            while (enumerator.DepartureTime < endTime)
            {
                count++;
                if (!enumerator.MoveNext())
                {
                    break;
                }
            }

            Assert.True(count > 0);
            TripHeadsignTest.Default.Run(db);
        }
    }
}