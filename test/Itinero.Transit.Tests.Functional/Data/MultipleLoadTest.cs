using System;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.CSA.Utils;
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
                sncb.AddAllConnectionsTo(w, start, end, Console.Error.WriteLine);
            }

            var db = new TransitDb(UpdateTimeFrame);

            var writer = db.GetWriter();
            sncb.AddAllLocationsTo(writer, Console.Error.WriteLine, null);
            writer.Close();


            db.UpdateTimeFrame(DateTime.Today, DateTime.Today.AddHours(24));
            Test(db);

            db.UpdateTimeFrame(DateTime.Today.AddHours(24), DateTime.Today.AddHours(48));
            Test(db);

            db.UpdateTimeFrame(DateTime.Today.AddHours(-24), DateTime.Today.AddHours(0));
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
                enumerator.MoveNext();
            }

            Assert.True(count > 0);
        }
    }
}