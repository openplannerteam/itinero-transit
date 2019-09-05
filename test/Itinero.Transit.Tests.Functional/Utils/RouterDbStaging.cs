using Itinero.Data.Graphs.Coders;
using Itinero.IO.Osm.Tiles;
using Itinero.Transit.Tests.Functional.Transfers;

namespace Itinero.Transit.Tests.Functional.Utils
{
    /// <summary>
    /// Initializes a routerDB that can be used by the other tests
    /// </summary>
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
                    ("pedestrian.weight", EdgeDataType.UInt32) ,
                    ("bicycle.weight", EdgeDataType.UInt32) // add one for each profile that is going to be used with name (profile).weight.
                })
            });
            routerDb.DataProvider = new DataProvider(routerDb);
            OsmTransferGenerator.EnableCaching("cache");

            RouterDb = routerDb;
        }
    }
}