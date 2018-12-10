using Itinero.IO.LC.Tests;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
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
            var transferGen = new InternalTransferGenerator(180);
            var c0 = connDb.LoadConnection(0);
            var c1 = connDb.LoadConnection(1);

            var root = new Journey<TransferStats>((0, 1), c0.DepartureTime, new TransferStats());
            var j = root.ChainForward(c0);
            var transfered = transferGen.CreateDepartureTransfer(j, c1);

            Assert.NotNull(transfered);
            Assert.True(transfered.PreviousLink.SpecialConnection);
            Assert.False(transfered.SpecialConnection);
            Assert.Equal(root, transfered.Root);
            Assert.Equal((uint) 1, transfered.Stats.NumberOfTransfers);


            // We need more time with luggage
            var transferWithLuggage = new InternalTransferGenerator(240);

            transfered = transferWithLuggage.CreateDepartureTransfer(j, c1);
            Assert.Null(transfered); // we didn't make the transfer!
        }
    }
}
