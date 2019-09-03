using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Tests.Functional.Utils;

namespace Itinero.Transit.Tests.Functional.Algorithms.Search
{
    public class StopSearchTest : 
        FunctionalTestWithInput<(TransitDb db, double lon, double lat, double distance)>
    {
        protected override void Execute()
        {
            Input.db.Latest.StopsDb.GetReader().FindClosest(new Stop(Input.lon, Input.lat), (uint) Input.distance);
        }
    }
}