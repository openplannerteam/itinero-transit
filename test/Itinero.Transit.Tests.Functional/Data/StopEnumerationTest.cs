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
            var reader = StopsReaderAggregator.CreateFrom(Input.Select(a => a.Latest));

            True(reader.MoveTo("https://data.delijn.be/stops/502132"));

            Information(reader.GlobalId);
            var n = reader.Attributes;
            n.TryGetValue("name", out var name);

            NotNull(name);
            True(reader.MoveTo("http://irail.be/stations/NMBS/008892007"));

            n = reader.Attributes;
            n.TryGetValue("name", out name);
            NotNull(name);

        }
    }
}