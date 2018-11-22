using Itinero.Transit;
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
            var storage = new LocalStorage(ResourcesTest.TestPath+"/SNCB/timetables");
            Assert.True(storage.KnownKeys().Count> 200);

            var prov = Belgium.Sncb(new LocalStorage(ResourcesTest.TestPath));

            var tt = ((LocallyCachedConnectionsProvider) (prov.ConnectionsProvider)).
                TimeTableContaining(ResourcesTest.TestMoment(10,00));
            Assert.NotNull(tt);
            Assert.Equal("http://graph.irail.be/sncb/connections?departureTime=2018-12-12T10:00:00.000Z",
                tt.Id().ToString());
        }


    }
}