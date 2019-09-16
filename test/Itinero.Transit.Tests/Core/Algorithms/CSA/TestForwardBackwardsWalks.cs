using System;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Utils;
using Xunit;

namespace Itinero.Transit.Tests.Core.Algorithms.CSA
{
    public class ConnectionScansWithDirectWalksTest
    {
        [Fact]
        public void LAS_vs_EAS_NoConnection_ExpectsSameDirectWalk()
        {
            // During functional testing, it turned out that walking from a stop was quicker then walking towards a stop
            // That was ofc an error, which caused EAS-LAS-comparison to fail in a very specific circumstance (if there was only a small window)
            // This is the reproduction of it


            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();

            var stop0 = wr.AddOrUpdateStop("0", 3.00, 50.00);
            var stop1 = wr.AddOrUpdateStop("1", 3.001, 50.001);

            wr.Close();

            var stops = tdb.Latest.StopsDb.GetReader();

            var d = (uint) DistanceEstimate.DistanceEstimateInMeter(50.00, 3.00f, 50.001, 3.001);

            Assert.True(d > 100);
            Assert.True(d < 250);

            var crow = new CrowsFlightTransferGenerator(speed: 1.0f);
            Assert.Equal(d, crow.TimeBetween(stops, stop0, stop1));
            Assert.Equal(d, crow.TimeBetween(stops, stop1, stop0));

            var tStart = DateTime.Now.ToUniversalTime().ToUnixTime();

            var input = tdb.SelectProfile(new Profile<TransferMetric>(
                    new InternalTransferGenerator(),
                    crow,
                    TransferMetric.Factory,
                    TransferMetric.ParetoCompare
                ))
                .SelectStops(stop0, stop1)
                .SelectTimeFrame(tStart.FromUnixTime(), (tStart + 1000).FromUnixTime());


            var eas = new EarliestConnectionScan<TransferMetric>(input.GetScanSettings());

            Assert.True(eas.JourneyFromDepartureTable.ContainsKey(stop0));
            Assert.True(eas.JourneyFromDepartureTable.ContainsKey(stop1));

            Assert.Equal(tStart, eas.JourneyFromDepartureTable[stop0].Time);
            Assert.Equal(tStart + d, eas.JourneyFromDepartureTable[stop1].Time);


            var las = new LatestConnectionScan<TransferMetric>(input.GetScanSettings());
            Assert.True(las.JourneysToArrivalStopTable.ContainsKey(stop0));
            Assert.True(las.JourneysToArrivalStopTable.ContainsKey(stop1));

            // LAS is backwards and thus uses the latest arrival time and arrival stop
            Assert.Equal(tStart + 1000, las.JourneysToArrivalStopTable[stop1].Time);
            Assert.Equal(tStart + 1000 - d, las.JourneysToArrivalStopTable[stop0].Time);
        }

        /// <summary>
        /// In some scenarios, the only option is to walk directly from A to B
        /// </summary>
        [Fact]
        public void EAS_TdbWithOneConnection_WalkingIsFaster_JourneyWithDirectWalk()
        {
            // During functional testing, it turned out that walking from a stop was quicker then walking towards a stop
            // That was ofc an error, which caused EAS-LAS-comparison to fail in a very specific circumstance (if there was only a small window)
            // This is the reproduction of it


            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();

            var stop0 = wr.AddOrUpdateStop("0", 3.00, 50.00);
            var stop1 = wr.AddOrUpdateStop("1", 3.00001, 50.00001);


            // Note that this connections falls out of the requested window
            wr.AddOrUpdateConnection(
                stop1, stop0, "qsdf", DateTime.Now.ToUniversalTime().AddMinutes(10),
                10 * 60, 0, 0, new TripId(0, 0), 0);
            wr.Close();


            var easJ = tdb.SelectProfile(
                    new DefaultProfile())
                .SelectStops(stop0, stop1)
                .SelectTimeFrame(DateTime.Now.ToUniversalTime(), DateTime.Now.AddHours(1).ToUniversalTime())
                .CalculateEarliestArrivalJourney();
            Assert.NotNull(easJ);
        }
    }
}