using Itinero.IO.LC.Tests;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Xunit;

namespace Itinero.Transit.Tests.Data
{
    public class WalkingGeneratorTest
    {
        [Fact]
        public void TestSimpleGenerator()
        {
            var connDb = Db.GetDefaultTestDb();

            // ReSharper disable once RedundantArgumentDefaultValue
            var transfGen = new InternalTransferGenerator(180);
            var c0 = connDb.LoadConnection(0);
            var c1 = connDb.LoadConnection(1);

            var root = new Journey<TransferStats>(0, c0.DepartureTime, new TransferStats());
            var j = root.ChainForward(c0);
            var jtransfered = transfGen.CreateDepartureTransfer(j, c1);

            Assert.NotNull(jtransfered);
            Assert.True(jtransfered.PreviousLink.SpecialConnection);
            Assert.False(jtransfered.SpecialConnection);
            Assert.Equal(root, jtransfered.Root);
            Assert.Equal((uint) 1, jtransfered.Stats.NumberOfTransfers);


            // We need more time with luggage
            var transferWithLuggage = new InternalTransferGenerator(240);

            jtransfered = transferWithLuggage.CreateDepartureTransfer(j, c1);
            Assert.Null(jtransfered); // we didn't make the transfer!
        }
    }
}
