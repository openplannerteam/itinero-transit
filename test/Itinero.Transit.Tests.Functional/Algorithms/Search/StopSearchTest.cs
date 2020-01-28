using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Tests.Functional.Utils;

namespace Itinero.Transit.Tests.Functional.Algorithms.Search
{
    public class StopSearchTest : 
        FunctionalTestWithInput<(TransitDb db, double lon, double lat, double distance)>
    {
        
        public override string Name => "Stop Search Test";

        
        protected override void Execute()
        {
            Input.db.Latest.Stops.FindClosest(new Stop("some stop",(Input.lon, Input.lat)), (uint) Input.distance);
        }
    }
}