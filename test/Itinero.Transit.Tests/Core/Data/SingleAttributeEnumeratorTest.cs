using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Xunit;

namespace Itinero.Transit.Tests.Core.Data
{
    public class SingleAttributeEnumeratorTest
    {
        private static void Test(IStopsDb stops, StopId id)
        {
            var source = stops.Get(id);
            string name = null;
            source.Attributes?.TryGetValue("name", out name);
            if (name != null)
            {
                Assert.NotEmpty(name);
            }

            if (source.Attributes == null) return;
            foreach (var (k, v) in source.Attributes)
            {
                if (!k.StartsWith("name:")) continue;
                Assert.StartsWith("name:", k);
                Assert.NotEmpty(v);
            }
        }

        [Fact]
        public void Enumerate_6Stops_AssertAllHaveName()
        {
            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();

            var a = wr.AddOrUpdateStop(new Stop("a", (1, 1)));
            var b = wr.AddOrUpdateStop(new Stop("b", (1, 1)));
            var c = wr.AddOrUpdateStop(new Stop("c", (1, 1), new Dictionary<string, string>
            {
                {"name", "c"}
            }));
            var d = wr.AddOrUpdateStop(new Stop("d", (1, 1), new Dictionary<string, string>
            {
                {"name", "d"},
                {"name:fr", "dfr"}
            }));
            var e = wr.AddOrUpdateStop(new Stop("e", (1, 1), new Dictionary<string, string>
            {
                {"name", "d"},
                {"name:", "d:"}
            }));
            var f = wr.AddOrUpdateStop(new Stop("f", (1, 1), new Dictionary<string, string>
            {
                {"bus", "yes"},
                {"name", "couseaukaai"},
                {"operator", "Stad Brugge"},
                {"public_transport", "stop_position"}
            }));
            tdb.CloseWriter();


            var stops = tdb.Latest.Stops;

            Test(stops, a);
            Test(stops, b);
            Test(stops, c);
            Test(stops, d);
            Test(stops, e);
            Test(stops, f);
        }
    }
}