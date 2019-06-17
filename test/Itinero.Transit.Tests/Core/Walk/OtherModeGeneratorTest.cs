using System;
using System.Collections.Generic;
using System.Threading;
using Itinero.Transit.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Utils;
using Xunit;

namespace Itinero.Transit.Tests.Core.Walk
{
    public class OtherModeGeneratorTest
    {
        [Fact]
        public void TestCrowsFlight()
        {
            var tdb = new TransitDb();
            var wr = tdb.GetWriter();
            var stop0 = wr.AddOrUpdateStop("0", 6, 50);
            var stop1 = wr.AddOrUpdateStop("1", 6.001, 50);
            wr.Close();

            var stops = tdb.Latest.StopsDb.GetReader();

            var exp = (uint) DistanceEstimate.DistanceEstimateInMeter(50, 6, 50, 6.001);

            var crow = new CrowsFlightTransferGenerator(speed: 1.0f);
            Assert.Equal(exp, crow.TimeBetween(stops, stop0, stop1));

            stops.MoveTo(stop1);

            var all = crow.TimesBetween(stops, (50,6), new List<IStop> {stops});
            Assert.Single(all);
            Assert.Equal(exp, all[stop1]);
        }

        [Fact]
        public void TestCrowsFlightJourneyBuilding()
        {
            var tdb = new TransitDb();
            var wr = tdb.GetWriter();
            var stop0 = wr.AddOrUpdateStop("0", 6, 50);
            var stop1 = wr.AddOrUpdateStop("1", 6.001, 50);
            wr.Close();

            var stops = tdb.Latest.StopsDb.GetReader();

            var exp = (uint) DistanceEstimate.DistanceEstimateInMeter(50, 6, 50, 6.001);

            var crow = new CrowsFlightTransferGenerator(speed: 1.0f);
            Assert.Equal(exp, crow.TimeBetween(stops, stop0, stop1));


            var genesis = new Journey<TransferMetric>(stop0, 0, TransferMetric.Factory);
            var j = genesis.ChainForwardWith(stops, crow, stop1);
            Assert.Equal(exp, j.Time);
        }


        [Fact]
        public void TestCaching()
        {
            var verySlow = new VerySlowOtherModeGenerator().UseCache();


            var time = DateTime.Now;

            var stop0 = new LocationId(0, 0, 0);
            var stop1 = new LocationId(0, 0, 1);
            var diff = verySlow.TimeBetween(new DummyReader(), stop0, stop1);
            var timeMid = DateTime.Now;
            Assert.True((timeMid - time).TotalMilliseconds >= 999);

            var diff0 = verySlow.TimeBetween(new DummyReader(), stop0, stop1);
            var timeEnd = DateTime.Now;
            Assert.Equal(diff, diff0);
            Assert.True((timeEnd - timeMid).TotalMilliseconds < 10);
        }
    }

    internal class VerySlowOtherModeGenerator : IOtherModeGenerator
    {
        public uint TimeBetween((double, double) __, IStop ___)
        {
            Thread.Sleep(1000);
            return 50;
        }

        public Dictionary<LocationId, uint> TimesBetween(IStopsReader reader, (double, double) @from,
            IEnumerable<IStop> to)
        {
            return this.DefaultTimesBetween(reader, from, to);
        }

        public float Range()
        {
            return 1000.0f;
        }
    }
}