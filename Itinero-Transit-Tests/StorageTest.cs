using System;
using System.Linq;
using Itinero_Transit.CSA;
using Itinero_Transit.CSA.ConnectionProviders;
using Itinero_Transit.CSA.Data;
using Itinero_Transit.LinkedData;
using Xunit;
using Xunit.Abstractions;

namespace Itinero_Transit_Tests
{
    public class StorageTest
    {
        private readonly ITestOutputHelper _output;

        public StorageTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestStorage()
        {
            var storage = new LocalStorage("test-storage");
            storage.ClearAll();
            storage.Store("1", "abc");
            var found = storage.Retrieve<string>("1");
            Assert.Equal(found, "abc");


            storage.Store("2", 42);
            var foundi = storage.Retrieve<int>("2");
            Assert.Equal(foundi, 42);
        }

        [Fact]
        public void TestStorageLocation()
        {
            var storage = new LocalStorage("timetables-for-testing-2018-10-02");
            Assert.NotEmpty(storage.KnownKeys());
            Log($"{storage.KnownKeys().Count()}");
            Assert.Equal(337, storage.KnownKeys().Count());
        }

        [Fact]
        public void TestSearchTimeTable()
        {
            var storage = new LocalStorage("timetables-for-testing-2018-10-02");
            Assert.Equal(339, storage.KnownKeys().Count());


            var prov = new LocallyCachedConnectionsProvider(new SncbConnectionProvider(), storage);

            var tt = prov.LatestTimeTableBefore(new DateTime(2018, 10, 02, 10, 00, 00, DateTimeKind.Local));
            Assert.Equal("http://graph.irail.be/sncb/connections?departureTime=2018-10-02T09:59:00.000Z",
                tt.Id().ToString());
        }

        // ReSharper disable once UnusedMember.Local
        private void Log(string s)
        {
            _output.WriteLine(s);
        }
    }
}