using System;
using Itinero.Transit.Data;
using Itinero.Transit.Journeys;
using Xunit;

namespace Itinero.Transit.Tests.Data
{
    public class JourneyTest
    {
        [Fact]
        public void TestSimpleJourney()
        {
            var connDb = new ConnectionsDb();
            var c0 = connDb.AddOrUpdate((0, 0), (0, 1),
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00),
                10 * 60, 0);

            var c1 = connDb.AddOrUpdate((0, 0), (0, 1),
                "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 16, 33, 00),
                10 * 60, 1);


            var time = new DateTime(2018, 12, 04, 16, 20, 00).ToUnixTime();
            var j = new Journey<TransferStats>((0, 0), time,
                new TransferStats());

            var reader = connDb.GetReader();
            reader.MoveTo(c0);

            j = j.ChainForward(reader);
            Assert.NotNull(j.Stats);
            Assert.Equal((uint) 10 * 60, j.Stats.TravelTime);
            Assert.Equal((uint) 0, j.Stats.NumberOfTransfers);


            reader.MoveTo(c1);
            j = j.TransferForward(reader);


            Assert.NotNull(j.Stats);
            Assert.Equal((uint) 23 * 60, j.Stats.TravelTime);
            Assert.Equal((uint) 1, j.Stats.NumberOfTransfers);

            Assert.Equal(reader.DepartureTime, j.DepartureTime());
        }

        [Fact]
        public void TestReverseJourney()
        {
            var connDb = new ConnectionsDb();
            var c0 = connDb.AddOrUpdate((0, 0), (0, 1),
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00),
                10 * 60, 0);

            var c1 = connDb.AddOrUpdate((0, 1), (0, 2),
                "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 16, 33, 00),
                10 * 60, 1);


            var time = new DateTime(2018, 12, 04, 16, 43, 00).ToUnixTime();
            var j = new Journey<TransferStats>((0, 2), time,
                new TransferStats());

            var reader = connDb.GetReader();
            reader.MoveTo(c1);

            j = j.ChainBackward(reader);

            j = j.ChainSpecial(Journey<TransferStats>.TRANSFER,
                new DateTime(2018, 12, 04, 16, 30, 00).ToUnixTime(),
                (0, 1), uint.MaxValue);

            reader.MoveTo(c0);
            j = j.ChainBackward(reader);


            var r = j.Reversed()[0];
            //Pr(" ---- Original ----");
            //Pr(j.ToString());
            //Pr(" ---- Reversed ----");
            //Pr(r.ToString());
            Assert.Equal(j.Root.Time, r.Time);
            Assert.Equal(r.Root.Time, j.Time);
            Assert.Equal(j.Root.Location, r.Location);
            Assert.Equal(r.Root.Location, j.Location);
            Assert.Equal(j.Stats, r.Stats);
        }
    }
}