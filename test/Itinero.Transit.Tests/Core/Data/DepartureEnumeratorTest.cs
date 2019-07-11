using System.Diagnostics;
using Itinero.Transit.Data;
using Xunit;

namespace Itinero.Transit.Tests.Core.Data
{
    public class DepartureEnumeratorTest
    {
        [Fact]
        public void TestReadWrite()
        {
            var tdb = new TransitDb(1);


            var wr = tdb.GetWriter();

            var stop0 = wr.AddOrUpdateStop("a", 50, 4);
            var stop1 = wr.AddOrUpdateStop("b", 51, 5);
            var input = new Connection(
                new ConnectionId(1, 0),
                "a", stop0, stop1, 12345, 6789, 5, 4, 1,
                new TripId(1, 5));
            wr.AddOrUpdateConnection(input);
            wr.Close();


            var reader = tdb.Latest.ConnectionsDb.GetReader();
            var f = reader.First().Value;
            var output = reader.Get(f);
            
            Assert.Equal(input.TravelTime, output.TravelTime);

            Assert.Equal(input, output);
        }

        [Fact]
        public void TestEnumeration()
        {
            var tdb = new TransitDb();


            var wr = tdb.GetWriter();

            var stop0 = wr.AddOrUpdateStop("a", 0, 0);
            var stop1 = wr.AddOrUpdateStop("b", 0, 0);

            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 1), "a", stop0, stop1, 50, 10, 0, 0, 0,
                new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 2), "b", stop0, stop1, 100, 10, 0, 0, 0,
                new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 3), "c", stop0, stop1, 1000, 10, 0, 0, 0,
                new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 4), "d", stop0, stop1, 1100, 10, 0, 0, 0,
                new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 5), "e", stop0, stop1, 1200, 10, 0, 0, 0,
                new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 6), "f", stop0, stop1, 1300, 10, 0, 0, 0,
                new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 7), "g", stop0, stop1, 1400, 10, 0, 0, 0,
                new TripId(0, 0)));
            wr.Close();


            var enumerator = tdb.Latest.ConnectionsDb.GetDepartureEnumerator();

            enumerator.MoveTo(1100);
            var c = new Connection();
            Assert.True(enumerator.HasNext());
            enumerator.Current(c);
            Assert.Equal((ulong) 1100, c.DepartureTime);
            Assert.Equal("d", c.GlobalId);
            Assert.True(enumerator.HasNext());
            enumerator.Current(c);
            Assert.Equal("e", c.GlobalId);
            Assert.True(enumerator.HasNext());
            enumerator.Current(c);


            Assert.Equal("f", c.GlobalId);

            Assert.True(enumerator.HasNext());
            enumerator.Current(c);


            Assert.Equal("g", c.GlobalId);
            Assert.False(enumerator.HasNext());

            Assert.True(enumerator.HasPrevious());
            Assert.True(enumerator.HasPrevious());
            enumerator.Current(c);
            Assert.Equal("f", c.GlobalId);

            Assert.True(enumerator.HasPrevious());
            enumerator.Current(c);
            Assert.Equal("e", c.GlobalId);
            Assert.True(enumerator.HasPrevious());
            enumerator.Current(c);
            Assert.Equal("d", c.GlobalId);
            Assert.True(enumerator.HasPrevious());
            enumerator.Current(c);
            Assert.Equal("c", c.GlobalId);
            Assert.True(enumerator.HasPrevious());
            enumerator.Current(c);
            Assert.Equal("b", c.GlobalId);
            Assert.True(enumerator.HasPrevious());
            enumerator.Current(c);
            Assert.Equal("a", c.GlobalId);
            Assert.True(enumerator.HasPrevious());
            enumerator.Current(c);

            enumerator.MoveTo(1001);
            Assert.True(enumerator.HasNext());
            enumerator.Current(c);
            Assert.Equal((ulong) 1100, c.DepartureTime);
            Assert.Equal("d", c.GlobalId);
        }
    }
}