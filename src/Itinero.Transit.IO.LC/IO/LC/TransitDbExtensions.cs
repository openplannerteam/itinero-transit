using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.IO.LC.Synchronization;
using Itinero.Transit.Logging;

namespace Itinero.Transit.IO.LC.IO.LC
{
    public static class TransitDbExtensions
    {
        /// <summary>
        /// Adds all linked connections of a given timeframe to the transitDB.
        /// The timeframe is calculated dynamically based on DateTime.Now
        /// </summary>
        /// <param name="tdb"></param>
        /// <param name="locationsUri"></param>
        /// <param name="connectionsUri"></param>
        /// <param name="loadBeforeNow"></param>
        /// <param name="loadAfterNow"></param>
        public static void UseLinkedConnections(
            this TransitDb tdb,
            string locationsUri,
            string connectionsUri,
            TimeSpan loadBeforeNow,
            TimeSpan loadAfterNow
        )
        {
            var now = DateTime.Now;
            tdb.UseLinkedConnections(locationsUri, connectionsUri,
                now - loadBeforeNow,
                now + loadAfterNow);
        }

        /// <summary>
        /// Adds all connections in a timewindow of the linked connections dataset to the transitdb.
        /// If an empty connectionsUri is given, only the locations will be loaded
        /// </summary>
        public static LinkedConnectionDataset UseLinkedConnections(this TransitDb tdb,
            string connectionsUri,
            string locationsUri,
            DateTime loadingStart,
            DateTime loadingEnd,
            bool onlyLoadLocations = false
        )
        {
            var lcDataset = new LinkedConnectionDataset(
                new Uri(connectionsUri),
                new Uri(locationsUri)
            );

            var writer = tdb.GetWriter();

            try
            {
                lcDataset.AddAllLocationsTo(writer,
                    Log.Warning);

                if (!onlyLoadLocations)
                {
                    lcDataset.AddAllConnectionsTo(writer, loadingStart, loadingEnd, Log.Warning);
                }
            }
            finally
            {
                writer.Close();
            }

            return lcDataset;
        }


        /// <summary>
        /// Adds a 'Linked Connection' dataset to the transitDB. The transitdb will automatically update as is specified by the syncPolicies
        /// </summary>
        public static (Synchronizer, LinkedConnectionDataset) UseLinkedConnections(this TransitDb tdb,
            string connectionsUri,
            string locationsUri,
            params SynchronizationPolicy[] syncPolicies)
        {
            return tdb.UseLinkedConnections(connectionsUri, locationsUri,
                new List<SynchronizationPolicy>(syncPolicies));
        }

        /// <summary>
        /// Adds a 'Linked Connection' dataset to the transitDB. The transitdb will automatically update as is specified by the syncPolicies.
        /// </summary>
        public static (Synchronizer, LinkedConnectionDataset) UseLinkedConnections(this TransitDb tdb,
            string connectionsUri,
            string locationsUri,
            List<SynchronizationPolicy> syncPolicies)
        {
            // Add all the locations to the tdb
            var dataset = tdb.UseLinkedConnections(connectionsUri, locationsUri, DateTime.MaxValue, DateTime.MinValue,
                onlyLoadLocations: true);

            // Merely initializing the synchronizer is enough to activate it
            // We return it though, e.g. if the user wants to query the loaded time frames
            return (new Synchronizer(tdb, dataset.UpdateTimeFrame, syncPolicies), dataset);
        }

        public static Synchronizer AddSyncPolicy(this TransitDb db, params SynchronizationPolicy[] policy)
        {
            return new Synchronizer(db, null, policy);
        }
    }
}