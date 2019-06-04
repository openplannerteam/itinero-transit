using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;

namespace Itinero.Transit.Tests.Functional.Data
{
    public class StopEnumerationTest : FunctionalTest<bool, List<TransitDb>>
    {
        protected override bool Execute(List<TransitDb> input)
        {
            var reader = StopsReaderAggregator.CreateFrom(input.Select(a => a.Latest));

            True(reader.MoveTo("https://data.delijn.be/stops/502132"));

            Information(reader.GlobalId);
            var n = reader.Attributes;
            n.TryGetValue("name", out var name);

            NotNull(name);
            True(reader.MoveTo("http://irail.be/stations/NMBS/008892007"));

            n = reader.Attributes;
            n.TryGetValue("name", out name);
            NotNull(name);

            return true;
        }
    }
}