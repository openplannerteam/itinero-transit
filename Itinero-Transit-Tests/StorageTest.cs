using System;
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
            Assert.Equal("abc",found);


            storage.Store("2", 42);
            // ReSharper disable once IdentifierTypo
            var foundi = storage.Retrieve<int>("2");
            Assert.Equal(42, foundi);
        }

        [Fact]
        public void TestStorageLocation()
        {
            var storage = new LocalStorage("timetables-for-testing-2018-10-17");
            Assert.NotEmpty(storage.KnownKeys());
            Log($"{storage.KnownKeys().Count}");
            Assert.True(storage.KnownKeys().Count > 330);
        }

        [Fact]
        public void TestSearchTimeTable()
        {
            var storage = new LocalStorage("timetables-for-testing-2018-10-17");
            Assert.Equal(341, storage.KnownKeys().Count);

            var loader = new Downloader();

            var sncb = new LinkedConnectionProvider(Sncb.HydraSearch(loader));
            var prov = new LocallyCachedConnectionsProvider(sncb, storage);

            var tt = prov.TimeTableContaining(new DateTime(2018, 10, 17, 10, 00, 00, DateTimeKind.Local));
            Assert.NotNull(tt);
            Assert.Equal("http://graph.irail.be/sncb/connections?departureTime=2018-10-17T09:58:00.000Z",
                tt.Id().ToString());
        }

        // ReSharper disable once UnusedMember.Local
        private void Log(string s)
        {
            _output.WriteLine(s);
        }
    }
}