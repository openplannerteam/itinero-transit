using System;
using System.Collections.Generic;
using Itinero.Transit.IO.LC.Synchronization;

namespace Itinero.Transit.Data
{
    public static class TransitDbExtensions
    {
        
        public static void UseOsmRoute(this TransitDb tdb, string url, DateTime start, DateTime end)
        {
            var r = OsmRoute.LoadFrom(url);
            foreach (var route in r)
            {
                tdb.UseOsmRoute(route, start, end);
            }
        }


        public static Synchronizer UseOsmRoute(this TransitDb tdb, string path,
            List<ISynchronizationPolicy> synchronizations)
        {
            var route = OsmRoute.LoadFrom(path);

            void Update(TransitDb.TransitDbWriter transitDbWriter,
                DateTime start, DateTime end)
            {
                foreach (var r in route)
                {
                    transitDbWriter.UseOsmRoute(r, start, end);
                }
            }

            return new Synchronizer(
                tdb,
                Update,
                synchronizations);
        }
    }
}