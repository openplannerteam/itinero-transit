using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Synchronization;
using Itinero.Transit.Journey;

namespace Itinero.Transit.IO.OSM.Data
{
    public static class TransitDbExtensions
    {
        /// <summary>
        /// Adds the capability to use floating locations (thus simple lat/lon-pairs).
        /// This can be done by using an openstreetmap-url as locations, e.g. "https://www.openstreetmap.org/#map=14/51.1985/3.2639"
        /// 
        /// Beware: the resulting object will keep all locations which were decoded in memory and allow them to
        /// be discovered by the routing algorithms. This is very useful to discover end- and startlocations,
        /// but might accumulate tons of locations if in continuous use.
        ///
        /// There is no need to invoke this method if using a selectLocation with (lat, lon)-combinations
        /// 
        /// </summary>
        /// <param name="withProfile"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static WithProfile<T> UseOsmLocations<T>(this WithProfile<T> withProfile) where T : IJourneyMetric<T>
        {
            var databaseIdCount = withProfile.DatabaseCount;

            var osmReader = new OsmLocationStopReader(databaseIdCount, hoard: true);
            return withProfile.AddStopsReader(osmReader);
        }


        public static IWithSingleLocation<T> SelectSingleStop<T>(
            this WithProfile<T> withProfile,
            (double latitude, double longitude) stop) where T : IJourneyMetric<T>
        {
            var databaseIdCount = withProfile.DatabaseCount;
            var osmReader = new OsmLocationStopReader(databaseIdCount);
            var fromId = osmReader.AddSearchableLocation(stop);

            var newProfile = withProfile.AddStopsReader(osmReader);
            return newProfile.SelectSingleStop(fromId);
        }


        public static IWithSingleLocation<T> SelectSingleStop<T>(
            this WithProfile<T> withProfile,
            IEnumerable<(double latitude, double longitude)> stops) where T : IJourneyMetric<T>
        {
            var databaseIdCount = withProfile.DatabaseCount;
            var osmReader = new OsmLocationStopReader(databaseIdCount);
            var fromIds = stops.Select(osmReader.AddSearchableLocation).ToList();

            var newProfile = withProfile.AddStopsReader(osmReader);
            return newProfile.SelectSingleStop(fromIds);
        }


        public static WithLocation<T> SelectStops<T>(
            this WithProfile<T> withProfile,
            (double latitude, double longitude) from,
            (double latitude, double longitude) to) where T : IJourneyMetric<T>
        {
            var databaseIdCount = withProfile.DatabaseCount;
            var osmReader = new OsmLocationStopReader(databaseIdCount);
            var fromId = osmReader.AddSearchableLocation(from);
            var toId = osmReader.AddSearchableLocation(to);

            var newProfile = withProfile.AddStopsReader(osmReader);
            return newProfile.SelectStops(fromId, toId);
        }


        public static WithLocation<T> SelectStops<T>(
            this WithProfile<T> withProfile,
            IEnumerable<(double latitude, double longitude)> from,
            IEnumerable<(double latitude, double longitude)> to) where T : IJourneyMetric<T>
        {
            var databaseIdCount = withProfile.DatabaseCount;
            var osmReader = new OsmLocationStopReader(databaseIdCount);
            var fromIds = from.Select(osmReader.AddSearchableLocation).ToList();
            var toIds = to.Select(osmReader.AddSearchableLocation).ToList();

            var newProfile = withProfile.AddStopsReader(osmReader);
            return newProfile.SelectStops(fromIds, toIds);
        }


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