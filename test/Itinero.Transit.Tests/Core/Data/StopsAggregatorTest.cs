using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Xunit;
// ReSharper disable RedundantArgumentDefaultValue

namespace Itinero.Transit.Tests.Algorithm.Data
{
    public class StopsAggregatorTest
    {
        [Fact]
        public void TestAggregator()
        {
            var tdb0 = new TransitDb(0);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop("b", 4.2, 4.100001);
            wr0.Close();


            var tdb1 = new TransitDb(1);
            var wr1 = tdb1.GetWriter();
            wr1.AddOrUpdateStop("a", 4.1, 4.1);
            wr1.Close();


            var results = tdb0.Latest.StopsDb.GetReader().SearchInBox((4, 4, 5, 5)).ToList();
            Assert.Single(results);

            results = tdb1.Latest.StopsDb.GetReader().SearchInBox((4, 4, 5, 5)).ToList();
            Assert.Single(results);


            var stopsReader = StopsReaderAggregator.CreateFrom(new[] {tdb0.Latest, tdb1.Latest});
            results = stopsReader.SearchInBox((4, 4, 5, 5)).ToList();
            Assert.Equal(2, results.Count());
        }
    }
}