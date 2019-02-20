using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC.CSA.ConnectionProviders;
using Itinero.Transit.IO.LC.CSA.LocationProviders;
using Itinero.Transit.IO.LC.CSA.Utils;
using Itinero.Transit.Logging;
using JsonLD.Core;

namespace Itinero.Transit.IO.LC.CSA
{
    /// <summary>
    /// A profile represents the preferences of the traveller.
    /// Which PT-operators does he want to take? Which doesn't he?
    /// How fast does he walk? All these are stored here
    /// </summary>
    public class LinkedConnectionDataset
    {
        public readonly List<ConnectionProvider> ConnectionsProvider;
        public readonly List<LocationProvider> LocationProvider;

        public LinkedConnectionDataset(ConnectionProvider[] connectionsProvider,
            LocationProvider[] locationProvider)
        {
            ConnectionsProvider = new List<ConnectionProvider>(connectionsProvider);
            LocationProvider = new List<LocationProvider>(locationProvider);
        }

        public LinkedConnectionDataset(List<LinkedConnectionDataset> sources)
        {
            ConnectionsProvider = new List<ConnectionProvider>();
            LocationProvider = new List<LocationProvider>();

            foreach (var p in sources)
            {
                ConnectionsProvider.AddRange(p.ConnectionsProvider);
                LocationProvider.AddRange(p.LocationProvider);
            }
        }


        /// <summary>
        ///  Creates a default profile, based on the locationsfragment-URL and conenctions-location fragment 
        /// </summary>
        /// <returns></returns>
        public LinkedConnectionDataset(
            Uri connectionsLink,
            Uri locationsUri
        )
        {
            var loader = new Downloader();

            var conProv = new ConnectionProvider
            (connectionsLink,
                connectionsLink + "{?departureTime}",
                loader);

            ConnectionsProvider = new List<ConnectionProvider> {conProv};

            // Create the locations provider

            var proc = new JsonLdProcessor(loader, locationsUri);
            var loc = new LocationProvider(locationsUri);
            loc.Download(proc);
            LocationProvider = new List<LocationProvider> {loc};
        }


        /// <summary>
        /// Adds all connection data between the given dates into the databases.
        /// Locations are only added as needed
        /// </summary>
        /// <param name="onTimeTableHandled">Callback when a timetable has been handled</param>
        public void AddAllConnectionsTo(TransitDb.TransitDbWriter writer,
            DateTime start, DateTime end, Action<string> onError,
            LoggingOptions onTimeTableHandled = null)
        {
            var dbs = new DatabaseLoader(writer, null, onTimeTableHandled, onError);
            dbs.AddAllConnections(this, start, end);
        }

        /// <summary>
        /// Adds all location data into the database
        /// </summary>
        /// <param name="linkedConnectionDataset"></param>
        /// <param name="transitDb"></param>
        /// <param name="onError"></param>
        /// <param name="onLocationHandled"></param>
        public void AddAllLocationsTo(
            TransitDb.TransitDbWriter writer,
            Action<string> onError,
            LoggingOptions onLocationHandled = null
        )
        {
            var dbs = new DatabaseLoader(writer, onLocationHandled, null, onError);
            dbs.AddAllLocations(this);
        }


        private static void OnLocationLoaded((int, int, int, int) status)
        {
            var (currentCount, batchTarget, batchCount, nrOfBatches) = status;
            Log.Information(
                $"Importing locations: Running batch {batchCount}/{nrOfBatches}: Importing location {currentCount}/{batchTarget}");
        }


        private static void OnConnectionLoaded((int, int, int, int) status)
        {
            var (currentCount, batchTarget, batchCount, nrOfBatches) = status;
            Log.Information(
                $"Importing connections: Running batch {batchCount}/{nrOfBatches}: Importing timetable {currentCount} (out of an estimated {batchTarget})");
        }

        public void UpdateTimeFrame(TransitDb.TransitDbWriter w, DateTime start, DateTime end)
        {
            Log.Information($"Loading time window {start}->{end}");
            AddAllConnectionsTo(w, start, end, Log.Warning, new LoggingOptions(OnConnectionLoaded, 1));
        }
    }
}