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
            wr0.AddOrUpdateStop(new Stop("b", 4.2, 4.100001));
            wr0.Close();


            var tdb1 = new TransitDb(1);
            var wr1 = tdb1.GetWriter();
            wr1.AddOrUpdateStop(new Stop("a", (4.1, 4.1)));
            wr1.Close();


            var searchAround = new Stop("x", (4.15, 4.15));

            var results = tdb0.Latest.StopsDb.GetInRange(searchAround, 50000);
            Assert.Single(results);

            results = tdb1.Latest.StopsDb.GetInRange(searchAround, 50000);
            Assert.Single(results);


            var stopsReader = StopsDbAggregator.CreateFrom(new[] {tdb0.Latest, tdb1.Latest});
            results = stopsReader.GetInRange(searchAround, 50000);
            Assert.Equal(2, results.Count());
        }

        [Fact]
        public void SearchInBox_ThreeStopsReaders_OneStop()
        {
            var tdb0 = new TransitDb(0);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop(new Stop("a", 4.0001, 4.100001));
            wr0.Close();


            var tdb1 = new TransitDb(1);
            var wr1 = tdb1.GetWriter();
            wr1.AddOrUpdateStop(new Stop("b", (4.1, 4.1)));
            wr1.Close();

            var tdb2 = new TransitDb(2);
            var wr2 = tdb2.GetWriter();
            wr2.AddOrUpdateStop(new Stop("c", (4.2, 4.2)));
            wr2.Close();


            var x = tdb0.Latest.StopsDb.GetInRange((4.1, 4.1), 500000).ToList();
            Assert.Single(x);


            x = tdb1.Latest.StopsDb.GetInRange((4.1, 4.1), 500000).ToList();
            Assert.Single(x);


            var cached = StopsDbAggregator.CreateFrom(new[] {tdb0.Latest, tdb1.Latest}).UseCache();
            var stopsReader = StopsDbAggregator.CreateFrom(
                    new List<IStopsDb>
                    {
                        cached,
                        tdb2.Latest.StopsDb
                    })
                ;


            var allresults = stopsReader.GetInRange((4.1, 4.1), 500000).ToList();
            Assert.Equal(3, allresults.Count);
            var allIds = allresults.Select(r => r.GlobalId).ToList();
            Assert.Contains("a", allIds);
            Assert.Contains("b", allIds);
            Assert.Contains("c", allIds);
        }

        [Fact]
        public void StopsAround_ThreeStopsReaders_ExpectsThreeStops()
        {
            var tdb0 = new TransitDb(0);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop(new Stop("a", 4.0001, 4.100001));
            wr0.Close();


            var tdb1 = new TransitDb(1);
            var wr1 = tdb1.GetWriter();
            wr1.AddOrUpdateStop(new Stop("b", 4.1, 4.1));
            wr1.Close();

            var tdb2 = new TransitDb(2);
            var wr2 = tdb2.GetWriter();
            wr2.AddOrUpdateStop(new Stop("c", 4.2, 4.2));
            wr2.Close();


            var stopsReader = StopsDbAggregator.CreateFrom(
                    new List<IStopsDb>
                    {
                        tdb0.Latest.StopsDb,
                        tdb1.Latest.StopsDb,
                        tdb2.Latest.StopsDb
                    });
            
            stopsReader.PostProcess(6);

            
            var results = stopsReader.GetInRange((4.1, 4.1), 250000);
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
            wr0.AddOrUpdateStop(new Stop("a", 4.0001, 4.100001));
            wr0.Close();


            var tdb1 = new TransitDb(1);
            var wr1 = tdb1.GetWriter();
            wr1.AddOrUpdateStop(new Stop("b", 4.1, 4.1));
            wr1.Close();


            var stopsDb = StopsDbAggregator.CreateFrom(
                    new List<IStopsDb>
                    {
                        tdb0.Latest.StopsDb,
                        tdb1.Latest.StopsDb
                    })
                ;


            Assert.Equal(2, stopsDb.Count());
        }

        [Fact]
        public void Enumerate_ThreeReaders_Expects3Stops()
        {
            var tdb0 = new TransitDb(0);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop(new Stop("a", 4.0001, 4.100001));
            wr0.Close();


            var tdb1 = new TransitDb(1);
            var wr1 = tdb1.GetWriter();
            wr1.AddOrUpdateStop(new Stop("b", 4.1, 4.1));
            wr1.Close();

            var tdb2 = new TransitDb(2);
            var wr2 = tdb2.GetWriter();
            wr2.AddOrUpdateStop(new Stop("c", 4.2, 4.2));
            wr2.Close();


            var stopsReader = StopsDbAggregator.CreateFrom(
                    new List<IStopsDb>
                    {
                        tdb0.Latest.StopsDb,
                        tdb1.Latest.StopsDb,
                        tdb2.Latest.StopsDb
                    });

            Assert.Equal(3, stopsReader.Count());
        }
        
        
        [Fact]
        public void Enumerate_ThreeReadersNonAdjacentIds_Expects3Stops()
        {
            var tdb0 = new TransitDb(5);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop(new Stop("a", 4.0001, 4.100001));
            wr0.Close();


            var tdb1 = new TransitDb(8);
            var wr1 = tdb1.GetWriter();
            wr1.AddOrUpdateStop(new Stop("b", 4.1, 4.1));
            wr1.Close();

            var tdb2 = new TransitDb(3);
            var wr2 = tdb2.GetWriter();
            wr2.AddOrUpdateStop(new Stop("c", 4.2, 4.2));
            wr2.Close();


            var stopsReader = StopsDbAggregator.CreateFrom(
                    new List<IStopsDb>
                    {
                        tdb0.Latest.StopsDb,
                        tdb1.Latest.StopsDb,
                        tdb2.Latest.StopsDb
                    });

            Assert.Equal(3, stopsReader.Count());
        }
        
        [Fact]
        public void MoveToGlobalId_ThreeReadersNonAdjacentIds_Expects3Stops()
        {
            var tdb0 = new TransitDb(5);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop(new Stop("a", 4.0001, 4.100001));
            wr0.Close();


            var tdb1 = new TransitDb(8);
            var wr1 = tdb1.GetWriter();
            wr1.AddOrUpdateStop(new Stop("b", 4.1, 4.1));
            wr1.Close();

            var tdb2 = new TransitDb(3);
            var wr2 = tdb2.GetWriter();
            wr2.AddOrUpdateStop(new Stop("c", 4.2, 4.2));
            wr2.Close();


            var stopsReader = StopsDbAggregator.CreateFrom(
                    new List<IStopsDb>
                    {
                        tdb0.Latest.StopsDb,
                        tdb1.Latest.StopsDb,
                        tdb2.Latest.StopsDb
                    });

           Assert.True(stopsReader.TryGet("a", out _));
           Assert.True(stopsReader.TryGet("b", out _));
           Assert.True(stopsReader.TryGet("c", out _));

        }

        [Fact]
        public void MoveTo_TwoDatabasesWithBigIds_NoCrash()
        {
            var tdb5 = new TransitDb(5);

            var wr = tdb5.GetWriter();
            var realStop = wr.AddOrUpdateStop(new Stop("stop5", 6.86, 51.684));
            wr.Close();

            var tdb10 = new TransitDb(10);
            var reader =
                StopsDbAggregator.CreateFrom(new List<IStopsDb>{tdb5.Latest.StopsDb, tdb10.Latest.StopsDb});
            Assert.False(reader.TryGet(new StopId(5, 12), out _));
            Assert.False(reader.TryGet(new StopId(0, 12), out _));
            Assert.False(reader.TryGet(new StopId(10, 12), out _));
            Assert.False(reader.TryGet(new StopId(11, 12), out _));
            Assert.False(reader.TryGet("abc", out _));
            Assert.True( reader.TryGet(realStop, out _));
            Assert.True( reader.TryGet("stop5", out _));
        }

        [Fact]
        public void Enumerate_ThreeReadersInAdvancedStructure_Expects3Stops()
        {
            var tdb0 = new TransitDb(0);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop(new Stop("a", 4.0001, 4.100001));
            wr0.Close();


            var tdb1 = new TransitDb(1);
            var wr1 = tdb1.GetWriter();
            wr1.AddOrUpdateStop(new Stop("b", 4.1, 4.1));
            wr1.Close();

            var tdb2 = new TransitDb(2);
            var wr2 = tdb2.GetWriter();
            wr2.AddOrUpdateStop(new Stop("c", 4.2, 4.2));
            wr2.Close();


            var stopsReader = StopsDbAggregator.CreateFrom(new List<IStopsDb>{
                StopsDbAggregator.CreateFrom(new List<IStopsDb>
                {
                    tdb0.Latest.StopsDb,
                    tdb1.Latest.StopsDb.UseCache()}),
                tdb2.Latest.StopsDb}
            );

            Assert.Equal(3, stopsReader.Count());
        }
    }
}