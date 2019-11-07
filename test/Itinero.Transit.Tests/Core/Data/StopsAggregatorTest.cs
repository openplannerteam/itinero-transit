using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Data.Core;
using Xunit;

namespace Itinero.Transit.Tests.Core.Data
{
    public class StopsAggregatorTest
    {
        [Fact]
        public void StopsAround_TwoStopsReaders_TwoStops()
        {
            var tdb0 = new TransitDb(0);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop("b", 4.2, 4.100001);
            wr0.Close();


            var tdb1 = new TransitDb(1);
            var wr1 = tdb1.GetWriter();
            wr1.AddOrUpdateStop("a", 4.1, 4.1);
            wr1.Close();


            var searchAround = new Stop(4.15, 4.15);

            var results = tdb0.Latest.StopsDb.GetReader().StopsAround(searchAround, 50000);
            Assert.Single(results);

            results = tdb1.Latest.StopsDb.GetReader().StopsAround(searchAround, 50000);
            Assert.Single(results);


            var stopsReader = StopsReaderAggregator.CreateFrom(new[] {tdb0.Latest, tdb1.Latest});
            results = stopsReader.StopsAround(searchAround, 50000);
            Assert.Equal(2, results.Count());
        }

        [Fact]
        public void SearchInBox_ThreeStopsReaders_OneStop()
        {
            var tdb0 = new TransitDb(0);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop("a", 4.0001, 4.100001);
            wr0.Close();


            var tdb1 = new TransitDb(1);
            var wr1 = tdb1.GetWriter();
            wr1.AddOrUpdateStop("b", 4.1, 4.1);
            wr1.Close();

            var tdb2 = new TransitDb(2);
            var wr2 = tdb2.GetWriter();
            wr2.AddOrUpdateStop("c", 4.2, 4.2);
            wr2.Close();


            var results = tdb0.Latest.StopsDb.GetReader().SearchInBox((4, 4, 5, 5)).ToList();
            Assert.Single(results);
            var x = tdb0.Latest.StopsDb.GetReader().StopsAround(new Stop(4.1, 4.1), 500000).ToList();
            Assert.Single(x);


            results = tdb1.Latest.StopsDb.GetReader().SearchInBox((4, 4, 5, 5)).ToList();
            Assert.Single(results);
            x = tdb1.Latest.StopsDb.GetReader().StopsAround(new Stop(4.1, 4.1), 500000).ToList();
            Assert.Single(x);


            var cached = StopsReaderAggregator.CreateFrom(new[] {tdb0.Latest, tdb1.Latest}).UseCache();
            var stopsReader = StopsReaderAggregator.CreateFrom(
                    new List<IStopsReader>
                    {
                        cached,
                        tdb2.Latest.StopsDb.GetReader()
                    })
                ;


            var allresults = stopsReader.StopsAround(new Stop(4.1, 4.1), 500000).ToList();
            Assert.Equal(3, allresults.Count);
            var allIds = allresults.Select(r => r.GlobalId);
            Assert.Contains("a", allIds);
            Assert.Contains("b", allIds);
            Assert.Contains("c", allIds);
        }

        [Fact]
        public void StopsAround_ThreeStopsReaders_ExpectsThreeStops()
        {
            var tdb0 = new TransitDb(0);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop("a", 4.0001, 4.100001);
            wr0.Close();


            var tdb1 = new TransitDb(1);
            var wr1 = tdb1.GetWriter();
            wr1.AddOrUpdateStop("b", 4.1, 4.1);
            wr1.Close();

            var tdb2 = new TransitDb(2);
            var wr2 = tdb2.GetWriter();
            wr2.AddOrUpdateStop("c", 4.2, 4.2);
            wr2.Close();


            var stopsReader = StopsReaderAggregator.CreateFrom(
                    new List<IStopsReader>
                    {
                        tdb0.Latest.StopsDb.GetReader(),
                        tdb1.Latest.StopsDb.GetReader(),
                        tdb2.Latest.StopsDb.GetReader()
                    })
                ;
            var results = stopsReader.StopsAround(new Stop(4.1, 4.1), 250000);
            Assert.Equal(3, results.Count());

            var ids = new HashSet<string>();
            ids.UnionWith(results.Select(x => x.GlobalId));
            Assert.Contains("a", ids);
            Assert.Contains("b", ids);
            Assert.Contains("c", ids);
        }

        [Fact]
        public void Enumerate_TwoStopsReaders_Expects2Stops()
        {
            var tdb0 = new TransitDb(0);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop("a", 4.0001, 4.100001);
            wr0.Close();


            var tdb1 = new TransitDb(1);
            var wr1 = tdb1.GetWriter();
            wr1.AddOrUpdateStop("b", 4.1, 4.1);
            wr1.Close();


            var stopsReader = StopsReaderAggregator.CreateFrom(
                    new List<IStopsReader>
                    {
                        tdb0.Latest.StopsDb.GetReader(),
                        tdb1.Latest.StopsDb.GetReader(),
                    })
                ;


            var sum = 0;
            stopsReader.Reset();
            while (stopsReader.MoveNext())
            {
                sum++;
            }

            Assert.Equal(2, sum);
        }

        [Fact]
        public void Enumerate_ThreeReaders_Expects3Stops()
        {
            var tdb0 = new TransitDb(0);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop("a", 4.0001, 4.100001);
            wr0.Close();


            var tdb1 = new TransitDb(1);
            var wr1 = tdb1.GetWriter();
            wr1.AddOrUpdateStop("b", 4.1, 4.1);
            wr1.Close();

            var tdb2 = new TransitDb(2);
            var wr2 = tdb2.GetWriter();
            wr2.AddOrUpdateStop("c", 4.2, 4.2);
            wr2.Close();


            var stopsReader = StopsReaderAggregator.CreateFrom(
                    new List<IStopsReader>
                    {
                        tdb0.Latest.StopsDb.GetReader(),
                        tdb1.Latest.StopsDb.GetReader(),
                        tdb2.Latest.StopsDb.GetReader()
                    })
                ;


            var sum = 0;
            stopsReader.Reset();
            while (stopsReader.MoveNext())
            {
                sum++;
            }

            Assert.Equal(3, sum);
        }
        
        
        [Fact]
        public void Enumerate_ThreeReadersNonAdjacentIds_Expects3Stops()
        {
            var tdb0 = new TransitDb(5);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop("a", 4.0001, 4.100001);
            wr0.Close();


            var tdb1 = new TransitDb(8);
            var wr1 = tdb1.GetWriter();
            wr1.AddOrUpdateStop("b", 4.1, 4.1);
            wr1.Close();

            var tdb2 = new TransitDb(3);
            var wr2 = tdb2.GetWriter();
            wr2.AddOrUpdateStop("c", 4.2, 4.2);
            wr2.Close();


            var stopsReader = StopsReaderAggregator.CreateFrom(
                    new List<IStopsReader>
                    {
                        tdb0.Latest.StopsDb.GetReader(),
                        tdb1.Latest.StopsDb.GetReader(),
                        tdb2.Latest.StopsDb.GetReader()
                    })
                ;


            var sum = 0;
            stopsReader.Reset();
            while (stopsReader.MoveNext())
            {
                sum++;
            }

            Assert.Equal(3, sum);
        }
        
        [Fact]
        public void MoveToGlobalId_ThreeReadersNonAdjacentIds_Expects3Stops()
        {
            var tdb0 = new TransitDb(5);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop("a", 4.0001, 4.100001);
            wr0.Close();


            var tdb1 = new TransitDb(8);
            var wr1 = tdb1.GetWriter();
            wr1.AddOrUpdateStop("b", 4.1, 4.1);
            wr1.Close();

            var tdb2 = new TransitDb(3);
            var wr2 = tdb2.GetWriter();
            wr2.AddOrUpdateStop("c", 4.2, 4.2);
            wr2.Close();


            var stopsReader = StopsReaderAggregator.CreateFrom(
                    new List<IStopsReader>
                    {
                        tdb0.Latest.StopsDb.GetReader(),
                        tdb1.Latest.StopsDb.GetReader(),
                        tdb2.Latest.StopsDb.GetReader()
                    })
                ;

           Assert.True(stopsReader.MoveTo("a"));
           Assert.True(stopsReader.MoveTo("b"));
           Assert.True(stopsReader.MoveTo("c"));

        }

        [Fact]
        public void MoveTo_TwoDatabasesWithBigIds_NoCrash()
        {
            var tdb5 = new TransitDb(5);

            var wr = tdb5.GetWriter();
            var realStop = wr.AddOrUpdateStop("stop5", 6.86, 51.684);
            wr.Close();

            var tdb10 = new TransitDb(10);
            var reader =
                StopsReaderAggregator.CreateFrom(tdb5.Latest.StopsDb.GetReader(), tdb10.Latest.StopsDb.GetReader());
            Assert.False(reader.MoveTo(new StopId(5, 10, 12)));
            Assert.False(reader.MoveTo(new StopId(0, 10, 12)));

            Assert.False(reader.MoveTo(new StopId(10, 10, 12)));
            Assert.False(reader.MoveTo(new StopId(11, 10, 12)));
            Assert.False(reader.MoveTo("abc"));

            Assert.True(reader.MoveTo(realStop));
            Assert.True(reader.MoveTo("stop5"));
        }

        [Fact]
        public void Enumerate_ThreeReadersInAdvancedStructure_Expects3Stops()
        {
            var tdb0 = new TransitDb(0);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop("a", 4.0001, 4.100001);
            wr0.Close();


            var tdb1 = new TransitDb(1);
            var wr1 = tdb1.GetWriter();
            wr1.AddOrUpdateStop("b", 4.1, 4.1);
            wr1.Close();

            var tdb2 = new TransitDb(2);
            var wr2 = tdb2.GetWriter();
            wr2.AddOrUpdateStop("c", 4.2, 4.2);
            wr2.Close();


            var stopsReader = StopsReaderAggregator.CreateFrom(
                StopsReaderAggregator.CreateFrom(
                    tdb0.Latest.StopsDb.GetReader(),
                    tdb1.Latest.StopsDb.GetReader().UseCache()),
                tdb2.Latest.StopsDb.GetReader()
            );


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