using Itinero.Transit.Data;
using Xunit;

namespace Itinero.Transit.Tests.Core.Data
{
    public class DepartureEnumeratorTest
    {
        [Fact]
        public void TestEnumeration()
        {
            var tdb = new TransitDb();


            var wr = tdb.GetWriter();

            var stop0 = wr.AddOrUpdateStop("a", 0, 0);
            var stop1 = wr.AddOrUpdateStop("b", 0, 0);

            wr.AddOrUpdateConnection("a",
                new SimpleConnection(0, "a", stop0, stop1, 50, 10, 0, 0, 0, new TripId(0, 0)));
            wr.AddOrUpdateConnection("b",
                new SimpleConnection(0, "b", stop0, stop1, 100, 10, 0, 0, 0, new TripId(0, 0)));
            wr.AddOrUpdateConnection("c",
                new SimpleConnection(0, "c", stop0, stop1, 1000, 10, 0, 0, 0, new TripId(0, 0)));

            wr.AddOrUpdateConnection("d",
                new SimpleConnection(0, "d", stop0, stop1, 1100, 10, 0, 0, 0, new TripId(0, 0)));
            wr.AddOrUpdateConnection("e",
                new SimpleConnection(0, "e", stop0, stop1, 1200, 10, 0, 0, 0, new TripId(0, 0)));
            wr.AddOrUpdateConnection("f",
                new SimpleConnection(0, "f", stop0, stop1, 1300, 10, 0, 0, 0, new TripId(0, 0)));
            wr.AddOrUpdateConnection("g",
                new SimpleConnection(0, "g", stop0, stop1, 1400, 10, 0, 0, 0, new TripId(0, 0)));
            wr.Close();


            var enumerator = tdb.Latest.ConnectionsDb.GetDepartureEnumerator();

            enumerator.MoveNext(1100);
            Assert.Equal((ulong) 1100, enumerator.DepartureTime);
            Assert.Equal("d", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("e", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("f", enumerator.GlobalId);
            Assert.True(enumerator.MoveNext());
            Assert.Equal("g", enumerator.GlobalId);
            Assert.False(enumerator.MoveNext());


            Assert.True(enumerator.MovePrevious());
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("f", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("e", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("d", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("c", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("b", enumerator.GlobalId);
            Assert.True(enumerator.MovePrevious());
            Assert.Equal("a", enumerator.GlobalId);
            Assert.False(enumerator.MovePrevious());
            
            enumerator.MoveNext(1001);
            Assert.Equal((ulong) 1100, enumerator.DepartureTime);
            Assert.Equal("d", enumerator.GlobalId);

            
            
        }
    }
}