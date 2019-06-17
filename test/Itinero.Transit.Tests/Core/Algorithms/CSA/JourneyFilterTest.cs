using System;
using Itinero.Transit.Data;
using Itinero.Transit.Journey.Filter;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Xunit;

namespace Itinero.Transit.Tests.Core.Algorithms.CSA
{
    public class JourneyFilterTest
    {
        [Fact]
        public void TestFiltering()
        {
            var tdb = new TransitDb();

            var writer = tdb.GetWriter();

            var stop0 = writer.AddOrUpdateStop("https://example.com/stops/0", 0, 0.0);
            var stop1 = writer.AddOrUpdateStop("https://example.com/stops/1", 0.1, 1.1);
            var stop2 = writer.AddOrUpdateStop("https://example.com/stops/2", 0.5, 0.5);
            var date = new DateTime(2018, 12, 04, 16, 00, 00, DateTimeKind.Utc);


            writer.AddOrUpdateConnection(stop0, stop1,
                "https://example.com/connections/0",
                new DateTime(2018, 12, 04, 16, 20, 00, DateTimeKind.Utc),
                10 * 60, 0, 0, new TripId(0, 0), 0);

            writer.AddOrUpdateConnection(stop1, stop2,
                "https://example.com/connections/1",
                new DateTime(2018, 12, 04, 16, 33, 00, DateTimeKind.Utc),
                10 * 60, 0, 0, new TripId(0, 1), 0);


            writer.AddOrUpdateConnection(stop0, stop2,
                "https://example.com/connections/2",
                new DateTime(2018, 12, 04, 16, 25, 00, DateTimeKind.Utc),
                30 * 60, 0, 0, new TripId(0, 2), 0);

            writer.Close();


      
            var routerWithTransfer = tdb.SelectProfile(new DefaultProfile())
                .SelectStops(stop0, stop2)
                .SelectTimeFrame(date, date.AddHours(1));


            var profile = new Profile<TransferMetric>(
                new InternalTransferGenerator(),
                new CrowsFlightTransferGenerator(),
                TransferMetric.Factory,
                TransferMetric.ProfileTransferCompare,
                new MaxNumberOfTransferFilter(0)
            );


            var router = tdb.SelectProfile(profile)
                .SelectStops(stop0, stop2)
                .SelectTimeFrame(date, date.AddHours(1));

           
            
            var pcsTr = routerWithTransfer.AllJourneys();
            Assert.Equal(2, pcsTr.Count);
            Assert.Equal((uint) 1, pcsTr[0].Metric.NumberOfTransfers);
            Assert.Equal((uint) 0, pcsTr[1].Metric.NumberOfTransfers);

            var pcs = router.AllJourneys();
            Assert.Single(pcs);
            Assert.Equal((uint)0, pcs[0].Metric.NumberOfTransfers);

           
        }
    }
}