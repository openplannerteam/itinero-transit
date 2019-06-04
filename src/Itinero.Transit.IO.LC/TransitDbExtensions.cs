using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Synchronization;

namespace Itinero.Transit.IO.LC
{
    public static class TransitDbExtensions
    {
        /// <summary>
        /// Adds all connections in a timewindow of the linked connections dataset to the transitdb.
        /// If an empty connectionsUri is given, only the locations will be loaded
        /// </summary>
        public static LinkedConnectionDataset UseLinkedConnections(this TransitDb tdb,
            string connectionsUri, string locationsUri,
            DateTime loadingStart, DateTime loadingEnd)
        {
            var lcDataset = new LinkedConnectionDataset(
                new Uri(connectionsUri),
                new Uri(locationsUri)
            );

            var writer = tdb.GetWriter();

            try
            {
                lcDataset.AddAllLocationsTo(writer);
                if (loadingStart < loadingEnd) lcDataset.AddAllConnectionsTo(writer, loadingStart, loadingEnd);
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
            params ISynchronizationPolicy[] syncPolicies)
        {
            return tdb.UseLinkedConnections(connectionsUri, locationsUri,
                new List<ISynchronizationPolicy>(syncPolicies));
        }

        /// <summary>
        /// Adds a 'Linked Connection' dataset to the transitDB. The transitdb will automatically update as is specified by the syncPolicies.
        /// </summary>
        public static (Synchronizer, LinkedConnectionDataset) UseLinkedConnections(this TransitDb tdb,
            string connectionsUri,
            string locationsUri,
            List<ISynchronizationPolicy> syncPolicies)
        {
            // Add all the locations to the tdb
            var dataset = tdb.UseLinkedConnections(connectionsUri, locationsUri, DateTime.MaxValue, DateTime.MinValue);

            // Merely initializing the synchronizer is enough to activate it
            // We return it though, e.g. if the user wants to query the loaded time frames
            return (new Synchronizer(tdb, dataset.UpdateTimeFrame, syncPolicies), dataset);
        }

        public static Synchronizer AddSyncPolicy(this TransitDb db, params ISynchronizationPolicy[] policy)
        {
            return new Synchronizer(db, null, policy);
        }
    }
}