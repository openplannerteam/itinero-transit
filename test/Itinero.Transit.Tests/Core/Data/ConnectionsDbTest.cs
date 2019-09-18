using System;
using System.IO;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Reminiscence.Arrays;
using Xunit;

namespace Itinero.Transit.Tests.Core.Data
{
    public class ConnectionsDbTest
    {
        [Fact]
        public void ConnectionsDb_WriteTo_ReadFrom_ExpectsSameResult()
        {
            var conn = new ConnectionsDb(1);

            var input = new Connection(
                new ConnectionId(1, 0),
                "XYZ", new StopId(1, 123, 0), new StopId(1, 456, 7),
                123456, 123, 2, 5, 3, new TripId(1, 2));
            conn.AddOrUpdate(input);

            using (var f = File.OpenWrite("Test.transitdb"))
            {
                conn.WriteTo(f);
            }

            ConnectionsDb read;
            using (var f = File.OpenRead("Test.transitdb"))
            {
                read = ConnectionsDb.ReadFrom(f, 1);
            }

            File.Delete("Test.transitdb");

            var index = read.First().Value;
            var output = read.Get(index);
            Assert.Equal(input, output);
            Assert.False(read.HasNext(index, out index));


            Assert.Equal(conn.EarliestDate, read.EarliestDate);
            Assert.Equal(conn.LatestDate, read.LatestDate);
            Assert.Equal(conn.NumberOfWindows, read.NumberOfWindows);
            Assert.Equal(conn.WindowSizeInSeconds, read.WindowSizeInSeconds);
            AssertArrayEquals(conn.Data, read.Data);

            AssertArrayEquals(conn.GlobalIdLinkedList, read.GlobalIdLinkedList);
            AssertArrayEquals(conn.DepartureWindowPointers, read.DepartureWindowPointers);
            AssertArrayEquals(conn.DeparturePointers, read.DeparturePointers);
            AssertArrayEquals(conn.GlobalIds, read.GlobalIds);

            Assert.Equal(conn.GlobalIdLinkedListPointer, read.GlobalIdLinkedListPointer);
        }

        private void AssertArrayEquals<T>(ArrayBase<T> a, ArrayBase<T> b)
        {
            if (a.Length != b.Length)
            {
                throw new Exception($"Sizes don't match: {a.Length}, {b.Length}");
            }

            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] == null && b[i] == null)
                {
                    continue;
                }

                if (a[i] == null && b[i].Equals(""))
                {
                    continue;
                }

                if (!a[i].Equals(b[i]))
                {
                    throw new Exception($"Index {i} doesn't match: {a[i]}, {b[i]}");
                }
            }
        }
    }
}