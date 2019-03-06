using System;
using Itinero.Transit.Data;
using Xunit;

namespace Itinero.Transit.Tests.Data
{
    public class LostDelayTest
    {
        public void AddConn(TransitDb tdb, int departureHour, int departureMinute,
            ushort travelTime,
            ushort depDelay, ushort arrDelay)
        {
            var writer = tdb.GetWriter();

            var depTime = new DateTime(2019, 03, 06, departureHour, departureMinute, 00);
            writer.AddOrUpdateConnection(
                (0, 0), (1, 1), "http://example.org/connection/0",
                depTime,
                travelTime, depDelay, arrDelay, 0);

            writer.Close();


            // And now we test
            var latest = tdb.Latest;
            var enumerator = latest.ConnectionsDb.GetDepartureEnumerator();
            Assert.True(enumerator.MoveNext(depTime));
            Assert.Equal(depTime.ToUnixTime(), enumerator.DepartureTime);
            Assert.Equal(depDelay, enumerator.DepartureDelay);
            Assert.Equal(arrDelay, enumerator.ArrivalDelay);
            Assert.Equal(travelTime, enumerator.TravelTime);

            Assert.False(enumerator.MoveNext());


            enumerator = latest.ConnectionsDb.GetDepartureEnumerator();
            Assert.True(enumerator.MovePrevious(depTime.AddMinutes(1)));
            Assert.False(enumerator.MovePrevious());
        }


        [Fact]
        public void TestDelays()
        {
            // We had a case where delay information disappears
            // We attempt to trigger it here

            var db = new TransitDb();

            AddConn(db, 10, 05, 60, 0, 0);
            AddConn(db, 10, 00, 55, 5, 0);
            AddConn(db, 10, 05, 60, 5, 5);
            AddConn(db, 10, 00, 60, 0, 5);
            AddConn(db, 10, 10, 60, 10, 10);

        }
    }
}