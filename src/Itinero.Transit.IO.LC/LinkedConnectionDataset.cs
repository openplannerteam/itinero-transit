using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC.Data;
using Itinero.Transit.IO.LC.Utils;
using Itinero.Transit.Logging;
using JsonLD.Core;

namespace Itinero.Transit.IO.LC
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


        private readonly Action<string> _onError;
        private readonly LoggingOptions _onLocationLoaded;
        private readonly LoggingOptions _onTimeTableLoaded;


        // Small helper constructor which handles the default values
        private LinkedConnectionDataset(LoggingOptions onLocationLoaded = null,
            LoggingOptions onTimeTableLoaded = null,
            Action<string> onError = null)
        {
            // Note the differences between _onTimeTableLoaded (the field), onTimeTableLoaded (the parameter) and onTimeTableLoaded (the function)
            _onLocationLoaded = onLocationLoaded ??
                                new LoggingOptions(OnLocationLoaded, 500);
            _onTimeTableLoaded = onTimeTableLoaded ??
                                  new LoggingOptions(OnTimeTableLoaded, 250);
            _onError = onError ?? Log.Verbose;
        }


        public LinkedConnectionDataset(ConnectionProvider[] connectionsProvider,
            LocationProvider[] locationProvider,
            LoggingOptions onLocationLoaded = null,
            LoggingOptions onTimeTableLoaded = null,
            Action<string> onError = null
        ) : this(onLocationLoaded, onTimeTableLoaded, onError)
        {
            ConnectionsProvider = new List<ConnectionProvider>(connectionsProvider);
            LocationProvider = new List<LocationProvider>(locationProvider);
        }

        public LinkedConnectionDataset(List<LinkedConnectionDataset> sources,
            LoggingOptions onLocationLoaded = null,
            LoggingOptions onTimeTableLoaded = null,
            Action<string> onError = null
        ) : this(onLocationLoaded, onTimeTableLoaded, onError)
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
            Uri locationsUri,
            LoggingOptions onLocationLoaded = null,
            LoggingOptions onTimeTableLoaded = null,
            Action<string> onError = null
        ) : this(onLocationLoaded, onTimeTableLoaded, onError)
        {

            var conProv = new ConnectionProvider
            (connectionsLink,
                connectionsLink + "{?departureTime}");
            ConnectionsProvider = new List<ConnectionProvider> {conProv};

            // Create the locations provider

            var proc = new JsonLdProcessor(new Downloader(), locationsUri);
            var loc = new LocationProvider(locationsUri);
            loc.Download(proc);
            LocationProvider = new List<LocationProvider> {loc};
        }


        /// <summary>
        /// Adds all connection data between the given dates into the databases.
        /// Locations are only added as needed
        /// </summary>
        public (int loaded, int reused) AddAllConnectionsTo(TransitDb.TransitDbWriter writer,
            DateTime start, DateTime end)
        {
            var dbs = new DatabaseLoader(writer, _onLocationLoaded, _onTimeTableLoaded, _onError);
            return dbs.AddAllConnections(this, start, end);
        }

        /// <summary>
        /// Adds all location data into the database
        /// </summary>
        public void AddAllLocationsTo(TransitDb.TransitDbWriter writer)
        {
            var dbs = new DatabaseLoader(writer, _onLocationLoaded, _onTimeTableLoaded, _onError);
            dbs.AddAllLocations(this);
        }


        private static void OnLocationLoaded((int, int, int, int) status)
        {
            var (currentCount, batchTarget, batchCount, nrOfBatches) = status;
            Log.Information(
                $"Importing locations: Running batch {batchCount + 1}/{nrOfBatches}: Importing location {currentCount}/{batchTarget}");
        }


        private static void OnTimeTableLoaded((int, int, int, int) status)
        {
            var (currentCount, batchTarget, batchCount, nrOfBatches) = status;
            Log.Information(
                $"Importing connections: Running batch {batchCount + 1}/{nrOfBatches}: Importing timetable {currentCount} (out of an estimated {batchTarget})");
        }

        public void UpdateTimeFrame(TransitDb.TransitDbWriter w, DateTime start, DateTime end
        )
        {
            Log.Information($"Loading time window {start}->{end}");
            AddAllConnectionsTo(w, start, end);
        }
    }
}