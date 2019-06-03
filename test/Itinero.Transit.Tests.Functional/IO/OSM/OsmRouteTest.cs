using Itinero.Transit.Data;

namespace Itinero.Transit.Tests.Functional.IO.OSM
{
    public class OsmRouteTest: FunctionalTest<bool, bool>
    {
        protected override bool Execute(bool input)
        {

            var routes = OsmRoute.LoadFrom("testdata/CentrumShuttle-Brugge.xml");
            True(routes.Count == 1);
            var route = routes[0];
            // 9 stops, but the first one == the last one
            True(route.StopPositions.Count == 10);
            return true;
        }
    }
}