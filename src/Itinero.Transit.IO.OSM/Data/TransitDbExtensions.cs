using System;
using System.Collections.Generic;
using Itinero.Transit.IO.LC.Synchronization;

namespace Itinero.Transit.Data
{
    public static class TransitDbExtensions
    {
        public static void UseOsmRoute(this TransitDb tdb, Uri url, DateTime start, DateTime end)
        {
            var r = OsmRoute.LoadFromUrl(url);
            foreach (var route in r)
            {
                tdb.UseOsmRoute(route, start, end);
            }
        }

        public static void UseOsmRoute(this TransitDb tdb, string filePath, DateTime start, DateTime end)
        {
            var r = OsmRoute.LoadFromFile(filePath);
            foreach (var route in r)
            {
                tdb.UseOsmRoute(route, start, end);
            }
        }

        public static void UseOsmRoute(this TransitDb tdb, long id, DateTime start, DateTime end)
        {
            var r = OsmRoute.LoadFromOsm(id);
            foreach (var route in r)
            {
                tdb.UseOsmRoute(route, start, end);
            }
        }

        public static Synchronizer UseOsmRoute(this TransitDb tdb, Uri url,
            List<ISynchronizationPolicy> synchronizations)
        {
            var route = OsmRoute.LoadFromUrl(url);

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