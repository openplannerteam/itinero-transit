using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Xunit;

namespace Itinero.Transit.Tests.Core.Data
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

        [Fact]
        public void TestAggregator1()
        {
            var tdb0 = new TransitDb(0);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop("a", 4.0001, 4.100001);
            wr0.Close();


            var tdb1 = new TransitDb(1);
            var wr1 = tdb1.GetWriter();
            wr1.AddOrUpdateStop("b", 4.1, 4.1);
            wr1.Close();

            var tdb2 = new TransitDb(1);
            var wr2 = tdb2.GetWriter();
            wr2.AddOrUpdateStop("c", 4.2, 4.2);
            wr2.Close();


            var results = tdb0.Latest.StopsDb.GetReader().SearchInBox((4, 4, 5, 5)).ToList();
            Assert.Single(results);

            results = tdb1.Latest.StopsDb.GetReader().SearchInBox((4, 4, 5, 5)).ToList();
            Assert.Single(results);


            var stopsReader = StopsReaderAggregator.CreateFrom(
                    new List<IStopsReader>
                    {
                        StopsReaderAggregator.CreateFrom(new[] {tdb0.Latest, tdb1.Latest}).UseCache(),
                        tdb2.Latest.StopsDb.GetReader()
                    })
                ;
            results = stopsReader.SearchInBox((4, 4, 5, 5)).ToList();
            Assert.Equal(3, results.Count);

            Assert.Equal("a", results[0].GlobalId);
            Assert.Equal("b", results[1].GlobalId);
            Assert.Equal("c", results[2].GlobalId);
            
            var sum = 0;
            stopsReader.Reset();
            while (stopsReader.MoveNext())
            {
                sum++;
            }
            Assert.Equal(3, sum);

        }
    }
}