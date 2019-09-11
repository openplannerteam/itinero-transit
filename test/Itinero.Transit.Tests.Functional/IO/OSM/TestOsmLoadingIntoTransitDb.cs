using System;
using Itinero.Transit.Data;
using Itinero.Transit.IO.OSM.Data;
using Itinero.Transit.Tests.Functional.Utils;

// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Tests.Functional.IO.OSM
{
    /// <summary>
    /// Tests loading of an osm relation
    /// </summary>
    public class TestOsmLoadingIntoTransitDb : FunctionalTestWithInput<(string url, int expectedNrOfStops)>
    {
        protected override void Execute()
        {
            var tdb = new TransitDb(0);
            tdb.UseOsmRoute(Input.url, DateTime.Now.Date.ToUniversalTime(),
                DateTime.Now.Date.AddDays(1).ToUniversalTime());


            // We also load them into the route object and check that the number of stops is correct

            var routes =
                OsmRoute.LoadFrom(Input.url);
            True(routes.Count == 1);
            var route = routes[0];
            // 9 stops, but the first one == the last one
            if (Input.expectedNrOfStops != 0)
            {
                True(route.StopPositions.Count == Input.expectedNrOfStops);
            }

            foreach (var stopPosition in route.StopPositions)
            {
                var (_, lon, lat, _) = stopPosition;
                True(lat > 49); // Lat
                True(lon < 6); // Lon
            }
        }
    }
}