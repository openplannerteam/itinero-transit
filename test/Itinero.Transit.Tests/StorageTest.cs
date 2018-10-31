using System;
using Itinero.Transit.CSA.ConnectionProviders;
using Itinero.Transit.CSA.Data;
using Itinero.Transit.LinkedData;
using Xunit;
using Xunit.Abstractions;

namespace Itinero.Transit_Tests
{
    public class StorageTest
    {
        private readonly ITestOutputHelper _output;

        public StorageTest(ITestOutputHelper output)
        {
            _output = output;
        }
        
        // ReSharper disable once UnusedMember.Local
        private void Log(string s)
        {
            _output.WriteLine(s);
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
        public void TestSearchTimeTable()
        {
            var storage = new LocalStorage(ResourcesTest.TestPath+"/sncb/timeTables");
            Assert.Equal(338, storage.KnownKeys().Count);

            var prov = Sncb.Profile(ResourcesTest.TestPath, "belgium.routerdb");

            var tt = ((LocallyCachedConnectionsProvider) (prov.ConnectionsProvider)).
                TimeTableContaining(ResourcesTest.TestMoment(10,00));
            Assert.NotNull(tt);
            Assert.Equal("https://graph.irail.be/sncb/connections?departureTime=2018-11-26T09:58:00.000Z",
                tt.Id().ToString());
        }


    }
}