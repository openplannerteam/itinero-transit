using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Xunit;

namespace Itinero.Transit.Tests.Core.Journey
{
    public class TransferMetricTest
    {
        // TEST ALT LINK WITH VEHICLE INCREASE TODO FIX

        [Fact]
        public void Construct_NormalCase_ExpectsCorrectMetrics()
        {
            var stop0 = new StopId(0, 0);
            var stop1 = new StopId(0, 1);
            var stop2 = new StopId(0, 2);

            var j = new Journey<TransferMetric>(stop0, 1000, TransferMetric.Factory);

            var c0 = new Connection(
                "a", stop0, stop1, 1000, 120, new TripId(0, 0));
            var j0 = j.ChainForward(new ConnectionId(0, 0), c0);
            var m0 = j0.Metric;

            Assert.Equal((uint) 120, m0.TravelTime);
            Assert.Equal((uint) 1, m0.NumberOfVehiclesTaken);
            Assert.Equal(0, m0.WalkingTime);

            var c1 = new Connection(
                "b", stop1, stop2, 1180, 600, new TripId(0, 0));
            var j1 = j0.ChainForward(new ConnectionId(0, 1), c1);
            var m1 = j1.Metric;

            Assert.Equal((uint) 780, m1.TravelTime);
            Assert.Equal((uint) 1, m1.NumberOfVehiclesTaken);
            Assert.Equal(0, m1.WalkingTime);
        }


        [Fact]
        public void Construct_WithTransfer_ExpectsCorrectMetrics()
        {
            var stop0 = new StopId(0, 0);
            var stop1 = new StopId(0, 1);
            var stop2 = new StopId(0, 2);

            var j = new Journey<TransferMetric>(stop0, 1000, TransferMetric.Factory);

            var c0 = new Connection(
                "a", stop0, stop1, 1000, 120, new TripId(0, 0));
            var j0 = j.ChainForward(new ConnectionId(0, 0), c0);
            var m0 = j0.Metric;

            Assert.Equal((uint) 120, m0.TravelTime);
            Assert.Equal((uint) 1, m0.NumberOfVehiclesTaken);
            Assert.Equal(0, m0.WalkingTime);

            // Different ID
            var c1 = new Connection(
                "b", stop1, stop2, 1180, 600, new TripId(0, 1));
            var j1 = j0.ChainForward(new ConnectionId(0, 1), c1);
            var m1 = j1.Metric;

            Assert.Equal((uint) 780, m1.TravelTime);
            Assert.Equal((uint) 2, m1.NumberOfVehiclesTaken);
            Assert.Equal(0, m1.WalkingTime);
        }

        [Fact]
        public void Construct_AlternativeLink_ExpectsCorrectMetrics()
        {
            var stop0 = new StopId(0, 0);
            var stop1 = new StopId(0, 1);
            var stop2 = new StopId(0, 2);

            var j = new Journey<TransferMetric>(stop0, 1000, TransferMetric.Factory);

            var c0 = new Connection(
                "a", stop0, stop1, 1000, 120, new TripId(0, 0));
            var j0 = j.ChainForward(new ConnectionId(0, 0), c0);


            var c1 = new Connection(
                "a", stop0, stop1, 1000, 120, new TripId(0, 0));
            var j1 = j.ChainForward(new ConnectionId(0, 0), c1);

            var jJoined = new Journey<TransferMetric>(j0, j1);

            var m = jJoined.Metric;

            Assert.Equal((uint) 120, m.TravelTime);
            Assert.Equal((uint) 1, m.NumberOfVehiclesTaken);
            Assert.Equal(0, m.WalkingTime);


            var c2 = new Connection(
                "b", stop1, stop2, 1180, 600, new TripId(0, 0));
            var j2 = jJoined.ChainForward(new ConnectionId(0, 1), c2);
            var m2 = j2.Metric;

            Assert.Equal((uint) 780, m2.TravelTime);
            Assert.Equal((uint) 1, m2.NumberOfVehiclesTaken);
            Assert.Equal(0, m2.WalkingTime);
        }
    }
}