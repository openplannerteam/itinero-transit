using System;
using CacheCow.Common.Helpers;
using Itinero.Transit.Data;

namespace Itinero.Transit.Tests.Functional.IO.OSM
{
    public class OsmRouteTest : FunctionalTest<bool, bool>
    {
        protected override bool Execute(bool input)
        {
            var routes =
                OsmRoute.LoadFrom(
                    "https://www.openstreetmap.org/relation/9413958#map=13/51.2007/3.2786&layers=N");
            True(routes.Count == 1);
            var route = routes[0];
            // 9 stops, but the first one == the last one
            Information($"The osmroute has {route.StopPositions.Count} stops");
            True(route.StopPositions.Count == 10);

            foreach (var stopPosition in route.StopPositions)
            {
                var (_, coor, _) = stopPosition;
                True(coor.Y > 49); // Lat
                True(coor.X < 6); // Lon
            }
            
            return true;
        }
    }
}