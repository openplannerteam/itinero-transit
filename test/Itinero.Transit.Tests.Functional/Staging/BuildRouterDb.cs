using System;
using System.IO;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using Serilog;

namespace Itinero.Transit.Tests.Functional.Staging
{
    /// <summary>
    /// Builds test routerdb's.
    /// </summary>
    public class BuildRouterDb
    {
        /// <summary>
        /// The local path of the routerdb.
        /// </summary>
        public static string LocalBelgiumRouterDb = "belgium.routerdb";
        
        /// <summary>
        /// Builds or loads a routerdb.
        /// </summary>
        /// <returns>The loaded routerdb.</returns>
        public static RouterDb BuildOrLoad()
        {
            Download.DownloadBelgiumAll();
            
            try
            {
                if (File.Exists(LocalBelgiumRouterDb))
                {
                    using (var stream = File.OpenRead(LocalBelgiumRouterDb))
                    {
                        return RouterDb.Deserialize(stream);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Existing RouterDb failed to load.", e);
            }
            
            Log.Information("RouterDb doesn't exist yet or failed to load, building...");
            var routerDb = new RouterDb();
            using (var stream = File.OpenRead(Download.BelgiumLocal))
            using (var outputStream = File.Open(LocalBelgiumRouterDb, FileMode.Create))
            {
                routerDb.LoadOsmData(stream, Vehicle.Pedestrian);
                routerDb.Serialize(outputStream);
            }

            return routerDb;
        }
    }
}