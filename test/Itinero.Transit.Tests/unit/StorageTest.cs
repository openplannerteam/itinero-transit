using Itinero.Transit.IO.LC.CSA.Utils;
using Itinero.Transit.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Itinero.IO.LC.Tests
{
    public class StorageTest : SuperTest
    {
        public StorageTest(ITestOutputHelper output) : base(output)
        {
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

    }
}
