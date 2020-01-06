using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Tests.Functional.Utils;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class StopEnumerationTest : FunctionalTestWithInput<List<TransitDb>>
    {
        protected override void Execute()
        {
            var reader = StopsDbAggregator.CreateFrom(Input.Select(a => a.Latest));

            True(reader.TryGet("https://data.delijn.be/stops/200372", out var stop));

            Information(stop.GlobalId);
            var n = stop.Attributes;
            n.TryGetValue("name", out var name);

            NotNull(name);
            True(reader.TryGet("http://irail.be/stations/NMBS/008892007", out stop));

            n = stop.Attributes;
            n.TryGetValue("name", out name);
            NotNull(name);

        }
    }
}