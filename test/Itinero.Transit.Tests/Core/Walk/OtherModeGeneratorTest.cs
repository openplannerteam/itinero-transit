using System;
using System.Collections.Generic;
using System.Threading;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Tests.Dummies;
using Itinero.Transit.Utils;
using Xunit;

namespace Itinero.Transit.Tests.Core.Walk
{
    public class OtherModeGeneratorTest
    {
        [Fact]
        public void FirstLastMilePolicy_TimeBetween_MultipleStops_ExpectsCorrectTimings()
        {
            var stop0 = new Stop("0",(0, 0));
            var stop1 = new Stop("1",(0.0001, 0));
            var stop2 = new Stop("2",(0.0002, 0));

            var mixed = new FirstLastMilePolicy(
                new FixedGenerator(1),
                new FixedGenerator(2),
                new List<Stop> {stop1},
                new FixedGenerator(3),
                new List<Stop> {stop2}
            );


            // Normal situation
            Assert.Equal((uint) 1, mixed.TimeBetween(stop0, stop1));
            // First mile
            Assert.Equal((uint) 2, mixed.TimeBetween(stop1, stop0));


            // Normal situation
            Assert.Equal((uint) 1, mixed.TimeBetween(stop2, stop1));
            // last mile
            Assert.Equal((uint) 3, mixed.TimeBetween(stop0, stop2));

            var timesBetween = mixed.TimesBetween(stop0, new List<Stop>
            {
                stop1,
                stop2
            });

            Assert.Equal((uint) 1,
                timesBetween[stop1]);
            Assert.Equal((uint) 3,
                timesBetween[stop2]);


            timesBetween = mixed.TimesBetween(stop1, new List<Stop>
            {
                stop0,
                stop2
            });

            Assert.Equal((uint) 2,
                timesBetween[stop0]);
            Assert.Equal((uint) 2,
                timesBetween[stop2]);
        }

        [Fact]
        public void CrowsFlight_TimesBetween_ExpectsCorrectTimes()
        {
            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();
            var stop0 = wr.AddOrUpdateStop(new Stop("0", (6, 50)));
            var stop1 = wr.AddOrUpdateStop(new Stop("1", (6.001, 50)));
            tdb.CloseWriter();

            var stops = tdb.Latest.Stops;

            var exp = (uint) DistanceEstimate.DistanceEstimateInMeter((6, 50), (6.001, 50));

            var crow = new CrowsFlightTransferGenerator(speed: 1.0f);
            Assert.Equal(exp, crow.TimeBetween(stops, stop0, stop1));

            var from = new Stop("qsdf", (6, 50));

            var all = crow.TimesBetween(from, new List<Stop> {stops.Get(stop1)});
            Assert.Single(all);
            Assert.Equal(exp, all[stops.Get(stop1)]);
        }

        [Fact]
        public void ChainForward_WithCrowsFlight_ExpectsCorrectTime()
        {
            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();
            var stop0 = wr.AddOrUpdateStop(new Stop("0", (6, 50)));
            var stop1 = wr.AddOrUpdateStop(new Stop("1", (6.001, 50)));
            tdb.CloseWriter();

            var stops = tdb.Latest.Stops;

            var exp = (uint) DistanceEstimate.DistanceEstimateInMeter((6, 50), (6.001, 50));

            var crow = new CrowsFlightTransferGenerator(speed: 1.0f);
            Assert.Equal(exp, crow.TimeBetween(stops, stop0, stop1));


            var genesis = new Journey<TransferMetric>(stop0, 0, TransferMetric.Factory);
            var j = genesis.ChainForwardWith(stops, crow, stop1);
            Assert.Equal(exp, j.Time);
        }


        [Fact]
        public void UseCache_TImeBetween_ExpectsCacheIsUsed()
        {
            var verySlow = new VerySlowOtherModeGenerator().UseCache();


            var time = DateTime.Now;

            var stop0 = new StopId(0, 0);
            var stop1 = new StopId(0, 1);
            var diff = verySlow.TimeBetween(new DummyStopsDb(), stop0, stop1);
            var timeMid = DateTime.Now;
            Assert.True((timeMid - time).TotalMilliseconds >= 999);

            var diff0 = verySlow.TimeBetween(new DummyStopsDb(), stop0, stop1);
            var timeEnd = DateTime.Now;
            Assert.Equal(diff, diff0);
            Assert.True((timeEnd - timeMid).TotalMilliseconds < 100);
        }
    }

    internal class VerySlowOtherModeGenerator : IOtherModeGenerator
    {
        public uint TimeBetween(Stop __, Stop ___)
        {
            Thread.Sleep(1000);
            return 50;
        }

        public Dictionary<Stop, uint> TimesBetween(Stop from,
            IEnumerable<Stop> to)
        {
            return this.DefaultTimesBetween(from, to);
        }

        public Dictionary<Stop, uint> TimesBetween(IEnumerable<Stop> from, Stop to)
        {
            return this.DefaultTimesBetween(from, to);
        }

        public uint Range()
        {
            return 1000;
        }

        public string OtherModeIdentifier()
        {
            return "test";
        }

        public IOtherModeGenerator GetSource(Stop from, Stop to)
        {
            return this;
        }
    }

    class FixedGenerator : IOtherModeGenerator
    {
        private readonly uint _time;

        public FixedGenerator(uint time)
        {
            _time = time;
        }

        public uint TimeBetween(Stop from, Stop to)
        {
            return _time;
        }

        public Dictionary<Stop, uint> TimesBetween(Stop from, IEnumerable<Stop> to)
        {
            return this.DefaultTimesBetween(from, to);
        }

        public Dictionary<Stop, uint> TimesBetween(IEnumerable<Stop> from, Stop to)
        {
            return this.DefaultTimesBetween(from, to);
        }

        public uint Range()
        {
            return uint.MaxValue;
        }

        public string OtherModeIdentifier()
        {
            return "test";
        }

        public IOtherModeGenerator GetSource(Stop from, Stop to)
        {
            return this;
        }
    }
}