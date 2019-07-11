using Itinero.Data.Graphs.Coders;
using Itinero.IO.Osm.Tiles;
using Itinero.Transit.IO.OSM;

namespace Itinero.Transit.Tests.Functional.Staging
{
    public static class RouterDbStaging
    {
        public static RouterDb RouterDb { get; private set; }

        public static void Setup()
        {
            var routerDb = new RouterDb(new RouterDbConfiguration()
            {
                Zoom = 14,
                EdgeDataLayout = new EdgeDataLayout(new (string key, EdgeDataType dataType)[]
                {
                    ("bicycle.weight", EdgeDataType.UInt32) // add one for each profile that is going to be used with name (profile).weight.
                })
            });
            routerDb.DataProvider = new DataProvider(routerDb);
            OsmTransferGenerator.EnableCaching("cache");

            RouterDbStaging.RouterDb = routerDb;
        }
    }
}