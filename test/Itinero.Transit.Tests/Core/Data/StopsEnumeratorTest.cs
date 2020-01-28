using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Xunit;

namespace Itinero.Transit.Tests.Core.Data
{
    public class StopsEnumeratorTest
    {
        [Fact]
        public void Enumerate_NoStops_Expects0()
        {
            var tdb0 = new TransitDb(0);

            var stopsDb = tdb0.Latest.Stops;

            Assert.Empty(stopsDb);
        }


        [Fact]
        public void Enumerate_OneStop_Expects1Stops()
        {
            var tdb0 = new TransitDb(0);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop(new Stop("a", (4.0001, 4.100001)));
            tdb0.CloseWriter();

            var stopsDb = tdb0.Latest.Stops;

            Assert.Single(stopsDb);
        }


        [Fact]
        public void Enumerate_TwoStops_Expects2Stops()
        {
            var tdb0 = new TransitDb(0);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop(new Stop("a", (4.0001, 4.100001)));
            wr0.AddOrUpdateStop(new Stop("b", (4.1, 4.1)));
            tdb0.CloseWriter();


            var stopsDb = tdb0.Latest.Stops;


            Assert.Equal(2, stopsDb.Count());
        }


        [Fact]
        public void Enumerate_ThreeStops_Expects3Stops()
        {
            var tdb0 = new TransitDb(0);
            var wr0 = tdb0.GetWriter();
            wr0.AddOrUpdateStop(new Stop("a", (4.0001, 4.100001)));
            wr0.AddOrUpdateStop(new Stop("b", (4.1, 4.1)));
            wr0.AddOrUpdateStop(new Stop("c", (4.5, 4.1)));
            tdb0.CloseWriter();

            var stopsDb = tdb0.Latest.Stops;


            Assert.Equal(3, stopsDb.Count());
        }
    }
}