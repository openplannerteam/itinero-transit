using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.Serialization;
using Itinero.Transit.Logging;
using Xunit;

namespace Itinero.Transit.Tests.Core.Data
{
    public class DepartureEnumeratorTest
    {
        [Fact]
        public void Enumeration_TestFirst_ExpectsFirstElement()
        {
            var tdb = new TransitDb(1);


            var wr = tdb.GetWriter();

            var stop0 = wr.AddOrUpdateStop(new Stop("a", (50, 4)));
            var stop1 = wr.AddOrUpdateStop(new Stop("b", (51, 5)));
            var input = new Connection(
                "a", stop0, stop1, 12345, 6789, 5, 4, 1,
                new TripId(1, 5));
            wr.AddOrUpdateConnection(input);
            wr.Close();


            var connectionsDb = tdb.Latest.ConnectionsDb;
            var output = connectionsDb.First();

            Assert.Equal(input.TravelTime, output.TravelTime);

            Assert.Equal(input, output);
        }

        [Fact]
        public void MoveNext_7Connections_AssertEnumeratesCorrectly()
        {
            var tdb = new TransitDb(0);


            var wr = tdb.GetWriter();

            var stop0 = wr.AddOrUpdateStop(new Stop("a", (0, 0)));
            var stop1 = wr.AddOrUpdateStop(new Stop("b", (0, 0)));

            wr.AddOrUpdateConnection(new Connection("a", stop0, stop1, 50, 10, 0, 0, 0, new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection("b", stop0, stop1, 100, 10, 0, 0, 0, new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection("c", stop0, stop1, 1000, 10, 0, 0, 0, new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection("d", stop0, stop1, 1100, 10, 0, 0, 0, new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection("e", stop0, stop1, 1200, 10, 0, 0, 0, new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection("f", stop0, stop1, 1300, 10, 0, 0, 0, new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection("g", stop0, stop1, 1400, 10, 0, 0, 0, new TripId(0, 0)));
            wr.Close();


            var cons = tdb.Latest.ConnectionsDb;
            var enumerator = cons.GetEnumeratorAt(1100);

            Assert.True(enumerator.MoveNext());
            var c = cons.Get(enumerator.Current);
            Assert.Equal((ulong) 1100, c.DepartureTime);
            Assert.Equal("d", c.GlobalId);
            Assert.True(enumerator.MoveNext());
            c = cons.Get(enumerator.Current);

            Assert.Equal("e", c.GlobalId);
            Assert.True(enumerator.MoveNext());
            c = cons.Get(enumerator.Current);


            Assert.Equal("f", c.GlobalId);

            Assert.True(enumerator.MoveNext());
            c = cons.Get(enumerator.Current);


            Assert.Equal("g", c.GlobalId);
            Assert.False(enumerator.MoveNext());


            enumerator = cons.GetEnumeratorAt(1460);

            Assert.True(enumerator.MovePrevious());
            c = cons.Get(enumerator.Current);

            Assert.Equal("g", c.GlobalId);
            Assert.True(enumerator.MovePrevious());
            c = cons.Get(enumerator.Current);
            Assert.Equal("f", c.GlobalId);

            Assert.True(enumerator.MovePrevious());
            c = cons.Get(enumerator.Current);
            Assert.Equal("e", c.GlobalId);
            Assert.True(enumerator.MovePrevious());
            c = cons.Get(enumerator.Current);
            Assert.Equal("d", c.GlobalId);
            Assert.True(enumerator.MovePrevious());
            c = cons.Get(enumerator.Current);
            Assert.Equal("c", c.GlobalId);
            Assert.True(enumerator.MovePrevious());
            c = cons.Get(enumerator.Current);
            Assert.Equal("b", c.GlobalId);
            Assert.True(enumerator.MovePrevious());
            c = cons.Get(enumerator.Current);
            Assert.Equal("a", c.GlobalId);
            Assert.False(enumerator.MovePrevious());

            enumerator = cons.GetEnumeratorAt(1460); // Should point to the last element to be given next
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void MoveNext_7Connections_AssertEnumeratesAll()
        {
            var tdb = new TransitDb(0);
            var d = (ulong) 24 * 60 * 60;


            var wr = tdb.GetWriter();

            var stop0 = wr.AddOrUpdateStop(new Stop("stop0", (0, 0)));
            var stop1 = wr.AddOrUpdateStop(new Stop("stop1", (0, 0)));

            wr.AddOrUpdateConnection(new Connection("a", stop0, stop1, d - 59, 1, 0, 0, 0, new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection("a0", stop0, stop1, d - 59, 2, 0, 0, 0, new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection("b", stop0, stop1, d + 1, 4, 0, 0, 0, new TripId(0, 1)));
            wr.AddOrUpdateConnection(new Connection("c", stop0, stop1, d + 61, 8, 0, 0, 0, new TripId(0, 2)));
            wr.AddOrUpdateConnection(new Connection("d", stop0, stop1, d + 121, 16, 0, 0, 0, new TripId(0, 3)));
            wr.AddOrUpdateConnection(new Connection("e", stop0, stop1, d + 121, 32, 0, 0, 0, new TripId(0, 3)));
            wr.Close();

            var cons = tdb.Latest.ConnectionsDb;

            var enumerator = cons.GetEnumeratorAt(0);

            var count = 0;
            var tt = 0;
            while (enumerator.MoveNext())
            {
                tt += cons.Get(enumerator.Current).TravelTime;
                count++;
            }

            Assert.Equal(6, count);
            Assert.Equal(63, tt);
        }


        [Fact]
        public void MovePrevious_7Connections_AssertEnumeratesAll()
        {
            var tdb = new TransitDb(0);
            var d = (ulong) 24 * 60 * 60;


            var wr = tdb.GetWriter();

            var stop0 = wr.AddOrUpdateStop(new Stop("stop0", (0, 0)));
            var stop1 = wr.AddOrUpdateStop(new Stop("stop1", (0, 0)));

            wr.AddOrUpdateConnection(new Connection("a", stop0, stop1, d - 59, 1, 0, 0, 0, new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection("a0", stop0, stop1, d - 59, 2, 0, 0, 0, new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection("b", stop0, stop1, d + 1, 4, 0, 0, 0, new TripId(0, 1)));
            wr.AddOrUpdateConnection(new Connection("c", stop0, stop1, d + 61, 8, 0, 0, 0, new TripId(0, 2)));
            wr.AddOrUpdateConnection(new Connection("d", stop0, stop1, d + 121, 16, 0, 0, 0, new TripId(0, 3)));
            wr.AddOrUpdateConnection(new Connection("e", stop0, stop1, d + 121, 32, 0, 0, 0, new TripId(0, 3)));
            wr.Close();

            var cons = tdb.Latest.ConnectionsDb;

            var enumerator = cons.GetEnumeratorAt(cons.LatestDate + 1);

            var tt = 0;
            var count = 0;
            while (enumerator.MovePrevious())
            {
                tt += cons.Get(enumerator.Current).TravelTime;

                count++;
            }

            Assert.Equal(6, count);
            Assert.Equal(63, tt);
        }

        [Fact]
        public void MoveNextMovePrevious_MoveToDateFirst_AssertEnumeratesCorrectly()
        {
            var tdb = new TransitDb(0);
            var tdb1 = new TransitDb(1);

            var d = (ulong) 24 * 60 * 60;


            var wr = tdb.GetWriter();
            var wr1 = tdb1.GetWriter();

            var stop0 = wr.AddOrUpdateStop(new Stop("a", (0, 0)));
            var stop1 = wr.AddOrUpdateStop(new Stop("b", (0, 0)));

            wr.AddOrUpdateConnection(new Connection("a", stop0, stop1, d - 59, 1, 0, 0, 0, new TripId(0, 0)));
            wr1.AddOrUpdateConnection(new Connection("a0", stop0, stop1, d - 59, 2, 0, 0, 0, new TripId(0, 0)));
            wr.AddOrUpdateConnection(new Connection("b", stop0, stop1, d + 1, 4, 0, 0, 0, new TripId(0, 1)));
            wr.AddOrUpdateConnection(new Connection("c", stop0, stop1, d + 61, 8, 0, 0, 0, new TripId(0, 2)));
            wr.AddOrUpdateConnection(new Connection("e", stop0, stop1, d + 121, 32, 0, 0, 0, new TripId(0, 3)));
            wr1.AddOrUpdateConnection(new Connection("d", stop0, stop1, d + 121, 16, 0, 0, 0, new TripId(0, 3)));
            wr.Close();
            wr1.Close();

            var expected = new[] {"a", "a0", "b", "c", "e", "d"};
            if (true)
            {
                var c0 = 0;
                var enumerator0 = tdb.Latest.ConnectionsDb.GetEnumeratorAt(d + 1000);
                while (enumerator0.MovePrevious())
                {
                    c0++;
                }

                Assert.Equal(4, c0);

                c0 = 0;
                enumerator0 = tdb.Latest.ConnectionsDb.GetEnumeratorAt(0);
                while (enumerator0.MoveNext())
                {
                    c0++;
                }

                Assert.Equal(4, c0);


                var c1 = 0;
                var enumerator1 = tdb1.Latest.ConnectionsDb.GetEnumeratorAt(d + 1000);
                while (enumerator1.MovePrevious())
                {
                    c1++;
                }

                Assert.Equal(2, c1);

                c1 = 0;
                enumerator1 = tdb1.Latest.ConnectionsDb.GetEnumeratorAt(0);
                while (enumerator1.MoveNext())
                {
                    c1++;
                }

                Assert.Equal(2, c1);
            }

            var mergedDb = ConnectionsDbAggregator.CreateFrom(new List<IConnectionsDb>
            {
                tdb.Latest.ConnectionsDb, tdb1.Latest.ConnectionsDb
            });

            var enumerator = mergedDb.GetEnumeratorAt(0);

            var count = 0;
            var tt = 0;
            while (enumerator.MoveNext())
            {
                var c = mergedDb.Get(enumerator.Current);
                tt += c.TravelTime;
                Assert.Equal(expected[count], c.GlobalId);

                count++;
            }

            Assert.Equal(6, count);
            Assert.Equal(63, tt);

            enumerator = mergedDb.GetEnumeratorAt(tdb.Latest.ConnectionsDb.LatestDate + 1);
            tt = 0;
            count = 0;
            while (enumerator.MovePrevious())
            {
                var c = mergedDb.Get(enumerator.Current);
                tt += c.TravelTime;
                Assert.Equal(expected[5 - count], c.GlobalId);

                count++;
            }

            Assert.Equal(6, count);
            Assert.Equal(63, tt);
        }

        [Fact]
        public void MoveNextMovePrevious_FromDiskTransitDb_AssertEnumeratesCorrectly()
        {
            var tdb = new TransitDb(0);
            var d = (ulong) 24 * 60 * 60;


            var wr = tdb.GetWriter();

            var stop0 = wr.AddOrUpdateStop(new Stop("a", (0, 0)));
            var stop1 = wr.AddOrUpdateStop(new Stop("b", (0, 0)));

            var tr0 = wr.AddOrUpdateTrip("0");
            var tr1 = wr.AddOrUpdateTrip("1");
            var tr2 = wr.AddOrUpdateTrip("2");
            var tr3 = wr.AddOrUpdateTrip("3");
            var tr4 = wr.AddOrUpdateTrip("4");
            
            wr.AddOrUpdateConnection(new Connection("a", stop0, stop1, d - 59, 1, 0, 0, 0,  tr0));
            wr.AddOrUpdateConnection(new Connection("a0", stop0, stop1, d - 59, 2, 0, 0, 0, tr0));
            wr.AddOrUpdateConnection(new Connection("b", stop0, stop1, d + 1, 4, 0, 0, 0,   tr1));
            wr.AddOrUpdateConnection(new Connection("c", stop0, stop1, d + 61, 8, 0, 0, 0,  tr2));
            wr.AddOrUpdateConnection(new Connection("d", stop0, stop1, d + 121, 16, 0, 0, 0,tr3));
            wr.AddOrUpdateConnection(new Connection("e", stop0, stop1, d + 121, 32, 0, 0, 0,tr4));
            wr.Close();


            using (var fOut = File.OpenWrite("TestEnum.transitdb"))
            {
                tdb.Latest.WriteTo(fOut);
            }

            using (var fIn = File.OpenRead("TestEnum.transitdb"))
            {
                tdb = new TransitDb(0);
                wr = tdb.GetWriter();
                wr.ReadFrom(fIn);
                wr.Close();
            }

            File.Delete("TestEnum.transitdb");


            var connections = tdb.Latest.ConnectionsDb;

            var enumerator = connections.GetEnumeratorAt(tdb.Latest.ConnectionsDb.EarliestDate);

            var count = 0;
            var tt = 0;
            while (enumerator.MoveNext())
            {
                var cId = enumerator.Current;
                var c = connections.Get(cId);
                tt += c.TravelTime;
                count++;
            }

            Assert.Equal(6, count);
            Assert.Equal(63, tt);

            enumerator = connections.GetEnumeratorAt(tdb.Latest.ConnectionsDb.LatestDate+1);
            tt = 0;
            count = 0;
            while (enumerator.MovePrevious())
            {
                var cId = enumerator.Current;
                var c = connections.Get(cId);
                tt += c.TravelTime;
                count++;
            }

            Assert.Equal(6, count);
            Assert.Equal(63, tt);
        }

        [Fact]
        public void MoveNextMovePrevious_MoveToDateFirst_AssertEnumeratesStopsAtEnd()
        {
            var tdb = new TransitDb(0);


            var wr = tdb.GetWriter();

            var stop0 = wr.AddOrUpdateStop(new Stop("a", (0, 0)));
            var stop1 = wr.AddOrUpdateStop(new Stop("b", (0, 0)));

            for (uint i = 0; i < 100000; i++)
            {
                wr.AddOrUpdateConnection(
                    new Connection("c" + i,
                        stop0, stop1,
                        50 + i * 1000, 10, 0, 0, 0,
                        new TripId(0, 0)));
            }

            wr.Close();


            var start = DateTime.Now;

            var enumerator = tdb.Latest.ConnectionsDb.GetEnumeratorAt(100000000 - 2000);
            var count = 0;
            while (enumerator.MoveNext() && enumerator.CurrentTime < 100000000)
            {
                count++;
            }

            var end = DateTime.Now;
            Log.Information($"{(end - start).TotalMilliseconds}ms needed");


            // Same, but in the other direction


            Assert.Equal(2, count);
            Assert.True((end - start).TotalMilliseconds < 5.0);

            start = DateTime.Now;
            enumerator = tdb.Latest.ConnectionsDb.GetEnumeratorAt(100000000);
            count = 0;
            while (enumerator.MovePrevious() && enumerator.CurrentTime >= 100000000 - 2000)
            {
                count++;
            }

            end = DateTime.Now;
            Log.Information($"{(end - start).TotalMilliseconds}ms needed (backwards)");

            Assert.Equal(2, count);
            Assert.True((end - start).TotalMilliseconds < 5.0);
        }
    }
}