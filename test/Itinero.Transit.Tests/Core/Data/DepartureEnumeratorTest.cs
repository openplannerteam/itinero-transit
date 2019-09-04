using System;
using System.IO;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Logging;
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


            var connectionsDb = tdb.Latest.ConnectionsDb;
            var f = connectionsDb.First().Value;
            var output = connectionsDb.Get(f);

            Assert.Equal(input.TravelTime, output.TravelTime);

            Assert.Equal(input, output);
        }

        [Fact]
        public void TestEnumeration()
        {
            var tdb = new TransitDb(0);


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
            Assert.True(enumerator.MoveNext());
            enumerator.Current(c);
            Assert.Equal((ulong) 1100, c.DepartureTime);
            Assert.Equal("d", c.GlobalId);
            Assert.True(enumerator.MoveNext());
            enumerator.Current(c);
            Assert.Equal("e", c.GlobalId);
            Assert.True(enumerator.MoveNext());
            enumerator.Current(c);


            Assert.Equal("f", c.GlobalId);

            Assert.True(enumerator.MoveNext());
            enumerator.Current(c);


            Assert.Equal("g", c.GlobalId);
            Assert.False(enumerator.MoveNext());


            enumerator.MoveTo(1460);
            Assert.True(enumerator.MovePrevious());
            enumerator.Current(c);
            Assert.Equal("g", c.GlobalId);
            Assert.True(enumerator.MovePrevious());
            enumerator.Current(c);
            Assert.Equal("f", c.GlobalId);

            Assert.True(enumerator.MovePrevious());
            enumerator.Current(c);
            Assert.Equal("e", c.GlobalId);
            Assert.True(enumerator.MovePrevious());
            enumerator.Current(c);
            Assert.Equal("d", c.GlobalId);
            Assert.True(enumerator.MovePrevious());
            enumerator.Current(c);
            Assert.Equal("c", c.GlobalId);
            Assert.True(enumerator.MovePrevious());
            enumerator.Current(c);
            Assert.Equal("b", c.GlobalId);
            Assert.True(enumerator.MovePrevious());
            enumerator.Current(c);
            Assert.Equal("a", c.GlobalId);
            Assert.False(enumerator.MovePrevious());

            enumerator.MoveTo(1001);
            Assert.True(enumerator.MoveNext());
            enumerator.Current(c);
            Assert.Equal((ulong) 1100, c.DepartureTime);
            Assert.Equal("d", c.GlobalId);
        }

        [Fact]
        public void TestEnumeration0()
        {
            var tdb = new TransitDb(0);
            var d = (ulong) 24 * 60 * 60;


            var wr = tdb.GetWriter();

            var stop0 = wr.AddOrUpdateStop("a", 0, 0);
            var stop1 = wr.AddOrUpdateStop("b", 0, 0);

            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 1), "a", stop0, stop1, d - 59, 1, 0, 0, 0,
                new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 6), "a0", stop0, stop1, d - 59, 2, 0, 0, 0,
                new TripId(0, 0)));

            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 2), "b", stop0, stop1, d + 1, 4, 0, 0, 0,
                new TripId(0, 1)));
            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 3), "c", stop0, stop1, d + 61, 8, 0, 0, 0,
                new TripId(0, 2)));
            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 4), "d", stop0, stop1, d + 121, 16, 0, 0, 0,
                new TripId(0, 3)));
            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 5), "e", stop0, stop1, d + 121, 32, 0, 0, 0,
                new TripId(0, 3)));
            wr.Close();


            var enumerator = tdb.Latest.ConnectionsDb.GetDepartureEnumerator();

            enumerator.MoveTo(tdb.Latest.ConnectionsDb.EarliestDate);

            var count = 0;
            var tt = 0;
            var c = new Connection();
            while (enumerator.MoveNext())
            {
                enumerator.Current(c);
                tt += c.TravelTime;
                count++;
            }

            Assert.Equal(6, count);
            Assert.Equal(63, tt);

            enumerator.MoveTo(tdb.Latest.ConnectionsDb.LatestDate);

            tt = 0;
            count = 0;
            while (enumerator.MovePrevious())
            {
                enumerator.Current(c);
                tt += c.TravelTime;

                count++;
            }

            Assert.Equal(6, count);
            Assert.Equal(63, tt);
        }

        [Fact]
        public void TestEnumerationAggregator()
        {
            var tdb = new TransitDb(0);
            var tdb1 = new TransitDb(0);

            var d = (ulong) 24 * 60 * 60;


            var wr = tdb.GetWriter();
            var wr1 = tdb1.GetWriter();

            var stop0 = wr.AddOrUpdateStop("a", 0, 0);
            var stop1 = wr.AddOrUpdateStop("b", 0, 0);

            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 1), "a", stop0, stop1, d - 59, 1, 0, 0, 0,
                new TripId(0, 0)));
            wr1.AddOrUpdateConnection(new Connection(new ConnectionId(0, 6), "a0", stop0, stop1, d - 59, 2, 0, 0, 0,
                new TripId(0, 0)));

            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 2), "b", stop0, stop1, d + 1, 4, 0, 0, 0,
                new TripId(0, 1)));
            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 3), "c", stop0, stop1, d + 61, 8, 0, 0, 0,
                new TripId(0, 2)));
            wr1.AddOrUpdateConnection(new Connection(new ConnectionId(0, 4), "d", stop0, stop1, d + 121, 16, 0, 0, 0,
                new TripId(0, 3)));
            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 5), "e", stop0, stop1, d + 121, 32, 0, 0, 0,
                new TripId(0, 3)));
            wr.Close();
            wr1.Close();

            var enumerator0 = tdb.Latest.ConnectionsDb.GetDepartureEnumerator();
            var enumerator1 = tdb1.Latest.ConnectionsDb.GetDepartureEnumerator();

            var enumerator =
                ConnectionEnumeratorAggregator.CreateFrom(enumerator0, enumerator1);

            enumerator.MoveTo(tdb.Latest.ConnectionsDb.EarliestDate);

            var count = 0;
            var tt = 0;
            var c = new Connection();
            while (enumerator.MoveNext())
            {
                enumerator.Current(c);
                tt += c.TravelTime;
                count++;
            }

            Assert.Equal(6, count);
            Assert.Equal(63, tt);

            enumerator.MoveTo(tdb.Latest.ConnectionsDb.LatestDate);
            tt = 0;
            count = 0;
            while (enumerator.MovePrevious())
            {
                enumerator.Current(c);
                tt += c.TravelTime;

                count++;
            }

            Assert.Equal(6, count);
            Assert.Equal(63, tt);
        }

        [Fact]
        public void TestEnumerationAfterRead()
        {
            var tdb = new TransitDb(0);
            var d = (ulong) 24 * 60 * 60;


            var wr = tdb.GetWriter();

            var stop0 = wr.AddOrUpdateStop("a", 0, 0);
            var stop1 = wr.AddOrUpdateStop("b", 0, 0);

            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 1), "a", stop0, stop1, d - 59, 1, 0, 0, 0,
                new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 6), "a0", stop0, stop1, d - 59, 2, 0, 0, 0,
                new TripId(0, 0)));

            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 2), "b", stop0, stop1, d + 1, 4, 0, 0, 0,
                new TripId(0, 1)));
            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 3), "c", stop0, stop1, d + 61, 8, 0, 0, 0,
                new TripId(0, 2)));
            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 4), "d", stop0, stop1, d + 121, 16, 0, 0, 0,
                new TripId(0, 3)));
            wr.AddOrUpdateConnection(new Connection(new ConnectionId(0, 5), "e", stop0, stop1, d + 121, 32, 0, 0, 0,
                new TripId(0, 4)));
            wr.Close();


            using (var fOut = File.OpenWrite("TestEnum.transitdb"))
            {
                tdb.Latest.WriteTo(fOut);
            }


            tdb = TransitDb.ReadFrom("TestEnum.transitdb", 0);
            File.Delete("TestEnum.transitdb");


            var enumerator = tdb.Latest.ConnectionsDb.GetDepartureEnumerator();

            enumerator.MoveTo(tdb.Latest.ConnectionsDb.EarliestDate);

            var count = 0;
            var tt = 0;
            var c = new Connection();
            while (enumerator.MoveNext())
            {
                enumerator.Current(c);
                tt += c.TravelTime;
                count++;
            }

            Assert.Equal(6, count);
            Assert.Equal(63, tt);

            enumerator.MoveTo(tdb.Latest.ConnectionsDb.LatestDate);

            tt = 0;
            count = 0;
            while (enumerator.MovePrevious())
            {
                enumerator.Current(c);
                tt += c.TravelTime;

                count++;
            }

            Assert.Equal(6, count);
            Assert.Equal(63, tt);
        }

        [Fact]
        public void TestEnumeratorNoOvershoot()
        {
            var tdb = new TransitDb(0);


            var wr = tdb.GetWriter();

            var stop0 = wr.AddOrUpdateStop("a", 0, 0);
            var stop1 = wr.AddOrUpdateStop("b", 0, 0);

            for (uint i = 0; i < 100000; i++)
            {
                wr.AddOrUpdateConnection(
                    new Connection(new ConnectionId(0, i), "a" + i,
                        stop0, stop1,
                        50 + i * 1000, 10, 0, 0, 0,
                        new TripId(0, 0)));
            }

            wr.Close();


            var enumerator = tdb.Latest.ConnectionsDb.GetDepartureEnumerator();

            var start = DateTime.Now;
            enumerator.MoveTo(100000000 - 2000);
            var count = 0;
            while (enumerator.MoveNext() && enumerator.CurrentDateTime < 100000000)
            {
                count++;
            }

            var end = DateTime.Now;
            Log.Information($"{(end - start).TotalMilliseconds}ms needed");


            // Same, but in the other direction


            Assert.Equal(2, count);
            Assert.True((end - start).TotalMilliseconds < 5.0);
            Assert.True(enumerator.NextWindowCounter < 100);

            start = DateTime.Now;
            enumerator.MoveTo(100000000);
            count = 0;
            while (enumerator.MovePrevious() && enumerator.CurrentDateTime >= 100000000 - 2000)
            {
                count++;
            }

            end = DateTime.Now;
            Log.Information($"{(end - start).TotalMilliseconds}ms needed (backwards)");

            Assert.Equal(2, count);
            Assert.True((end - start).TotalMilliseconds < 5.0);
            Assert.True(enumerator.NextWindowCounter < 100);
        }
    }
}