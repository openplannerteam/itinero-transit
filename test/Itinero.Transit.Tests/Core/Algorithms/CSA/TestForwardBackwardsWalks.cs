using System;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Utils;
using Xunit;

namespace Itinero.Transit.Tests.Core.Algorithms.CSA
{
    public class TestForwardBackwardsWalks
    {
        [Fact]
        public void ForwardBackwardsWalksTest()
        {
            // During functional testing, it turned out that walking from a stop was quicker then walking towards a stop
            // That was ofc an error, which caused EAS-LAS-comparison to fail in a very specific circumstance (if there was only a small window)
            // This is the reproduction of it


            var tdb = new TransitDb();
            var wr = tdb.GetWriter();

            var stop0 = wr.AddOrUpdateStop("0", 3.00, 50.00);
            var stop1 = wr.AddOrUpdateStop("1", 3.001, 50.001);

            wr.Close();

            var stops = tdb.Latest.StopsDb.GetReader();

            var d = (uint) stops.CalculateDistanceBetween(stop0, stop1);

            Assert.True(d > 100);

            var crow = new CrowsFlightTransferGenerator(speed: 1.0f);
            Assert.Equal(d, crow.TimeBetween(stops, stop0, stop1));
            Assert.Equal(d, crow.TimeBetween(stops, stop1, stop0));

            var tStart = DateTime.Now.ToUniversalTime().ToUnixTime();
            
            var input = tdb.SelectProfile(new Profile<TransferMetric>(
                    new InternalTransferGenerator(),
                    crow,
                    TransferMetric.Factory,
                    TransferMetric.ProfileTransferCompare
                ))
                .SelectStops(stop0, stop1)
                .SelectTimeFrame(tStart.FromUnixTime(), (tStart+1000).FromUnixTime());
            
            
            var eas = new EarliestConnectionScan<TransferMetric>(input.GetScanSettings());
            
            Assert.True(eas.JourneyFromDepartureTable.ContainsKey(stop0));
            Assert.True(eas.JourneyFromDepartureTable.ContainsKey(stop1));

            Assert.Equal(tStart, eas.JourneyFromDepartureTable[stop0].Time);
            Assert.Equal(tStart+d, eas.JourneyFromDepartureTable[stop1].Time);

            
            var las = new LatestConnectionScan<TransferMetric>(input.GetScanSettings());
            Assert.True(las.JourneysToArrivalStopTable.ContainsKey(stop0));
            Assert.True(las.JourneysToArrivalStopTable.ContainsKey(stop1));

            // LAS is backwards and thus uses the latest arrival time and arrival stop
            Assert.Equal(tStart+1000, las.JourneysToArrivalStopTable[stop1].Time);
            Assert.Equal(tStart+1000-d, las.JourneysToArrivalStopTable[stop0].Time);


            
        }
    }
}