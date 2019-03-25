using System;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Xunit;

namespace Itinero.Transit.Tests.Data.Aggregators
{
    public class AggregatorTest
    {
        [Fact]
        public void TestAggregator()
        {
            var tdb0 = new TransitDb();
            var tdb1 = new TransitDb();
            var wr0 = tdb0.GetWriter();
            var wr1 = tdb1.GetWriter();

            var stop00 = wr0.AddOrUpdateStop("http://stop0.be", 0.0, 0.0);
            var stop01 = wr0.AddOrUpdateStop("http://stop1.be", 1.0, 0.0);
            var stop10 = wr1.AddOrUpdateStop("http://stop0.nl", 1.0, 1.0);
            var stop11 = wr1.AddOrUpdateStop("http://stop1.nl", 0.0, 1.0);

            wr0.AddOrUpdateConnection(stop00, stop01, "conn0", new DateTime(2019, 03, 19, 10, 00, 00), 30, 0, 0, 0, 0);
            wr0.AddOrUpdateConnection(stop00, stop01, "conn2", new DateTime(2019, 03, 19, 10, 02, 00), 30, 0, 0, 0, 0);
            wr0.AddOrUpdateConnection(stop00, stop01, "conn4", new DateTime(2019, 03, 19, 10, 04, 00), 30, 0, 0, 0, 0);
            wr0.AddOrUpdateConnection(stop00, stop01, "conn6", new DateTime(2019, 03, 19, 10, 06, 00), 30, 0, 0, 0, 0);
            wr0.AddOrUpdateConnection(stop00, stop01, "conn8", new DateTime(2019, 03, 19, 10, 08, 00), 30, 0, 0, 0, 0);
            wr0.AddOrUpdateConnection(stop00, stop01, "conn10", new DateTime(2019, 03, 19, 10, 10, 00), 30, 0, 0, 0, 0);
            wr1.AddOrUpdateConnection(stop10, stop11, "conn1", new DateTime(2019, 03, 19, 10, 01, 00), 30, 0, 0, 0, 0);
            wr1.AddOrUpdateConnection(stop10, stop11, "conn3", new DateTime(2019, 03, 19, 10, 03, 00), 30, 0, 0, 0, 0);
            wr1.AddOrUpdateConnection(stop10, stop11, "conn5", new DateTime(2019, 03, 19, 10, 05, 00), 30, 0, 0, 0, 0);
            wr1.AddOrUpdateConnection(stop10, stop11, "conn7", new DateTime(2019, 03, 19, 10, 07, 00), 30, 0, 0, 0, 0);
            wr1.AddOrUpdateConnection(stop10, stop11, "conn9", new DateTime(2019, 03, 19, 10, 09, 00), 30, 0, 0, 0, 0);
            wr1.AddOrUpdateConnection(stop10, stop11, "conn11", new DateTime(2019, 03, 19, 10, 11, 00), 30, 0, 0, 0, 0);

            wr0.Close();
            wr1.Close();


            var aggr = new ConnectionEnumeratorAggregator(
                tdb0.Latest.ConnectionsDb.GetDepartureEnumerator(),
                tdb1.Latest.ConnectionsDb.GetDepartureEnumerator());
            
            Assert.True(aggr.MoveNext(new DateTime(2019, 03, 19, 9, 59, 00)));

            var i = 0;
            do
            {

                var c = aggr;
                Assert.Equal(i, c.DepartureTime.FromUnixTime().Minute);
                if (i >= 12)
                {
                    throw new Exception("To much connections");
                }
                i++;
            } while (aggr.MoveNext());
            Assert.Equal(12, i);

        }
    }
}