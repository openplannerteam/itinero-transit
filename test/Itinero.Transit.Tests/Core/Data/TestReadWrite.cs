using System;
using System.IO;
using Itinero.Transit.Data;
using Reminiscence.Arrays;
using Xunit;

namespace Itinero.Transit.Tests.Core.Data
{
    public class TestReadWrite
    {
        [Fact]
        public void TestConnectionsDbReadWrite()
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

            var index = read.GetReader().First().Value;
            var output = read.GetReader().Get(index);
            Assert.Equal(input, output);
            Assert.False(read.GetReader().HasNext(index, out index));


            Assert.Equal(conn.EarliestDate, read.EarliestDate);
            Assert.Equal(conn.LatestDate, read.LatestDate);
            Assert.Equal(conn._numberOfWindows, read._numberOfWindows);
            Assert.Equal(conn._windowSizeInSeconds, read._windowSizeInSeconds);
            AssertArrayEquals(conn._data, read._data);

            AssertArrayEquals(conn._globalIdLinkedList, read._globalIdLinkedList);
            AssertArrayEquals(conn._departureWindowPointers, read._departureWindowPointers);
            AssertArrayEquals(conn._departurePointers, read._departurePointers);
            AssertArrayEquals(conn._globalIds, read._globalIds);

            Assert.Equal(conn._globalIdLinkedListPointer, read._globalIdLinkedListPointer);
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