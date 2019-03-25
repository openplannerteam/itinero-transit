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
            var tdb = Db.GetDefaultTestDb(out var stop0, out var stop1, out var stop2, out var _, out var _, out var _);

            var connDb = tdb.Latest.ConnectionsDb;

            // ReSharper disable once RedundantArgumentDefaultValue
            var transferGen = new InternalTransferGenerator(180);
            var c0 = connDb.GetReader();
            c0.MoveTo("https://example.com/connections/0");
            var c1 = connDb.GetReader();
            c1.MoveTo("https://example.com/connections/1");

            var root = new Journey<TransferStats>(c0.DepartureStop, c0.DepartureTime, TransferStats.Factory);
            var j = root.ChainForward(c0);
            var transfered = transferGen
                .CreateDepartureTransfer(null, j, c1.DepartureTime, c1.DepartureStop);
            Assert.NotNull(transfered);
            transfered = transfered.ChainForward(c1);

            Assert.NotNull(transfered);
            Assert.True(transfered.PreviousLink.SpecialConnection);
            Assert.False(transfered.SpecialConnection);
            Assert.Equal(root, transfered.Root);
            Assert.Equal((uint) 1, transfered.Stats.NumberOfTransfers);

            // We need more time with luggage
            var transferWithLuggage = new InternalTransferGenerator(240);

            transfered = transferWithLuggage
                .CreateDepartureTransfer(null, j, c1.DepartureTime, c1.DepartureStop)
                ?.ChainForward(c1);
            Assert.Null(transfered); // we didn't make the transfer!
        }
    }
}