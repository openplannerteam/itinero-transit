using System;
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
        public readonly ConnectionProvider ConnectionsProvider;
        public readonly LocationFragment LocationProvider;


        // Small helper constructor which handles the default values
        private LinkedConnectionDataset()
        {
        }


        /// <summary>
        ///  Creates a default profile, based on the locationsfragment-URL and conenctions-location fragment 
        /// </summary>
        /// <returns></returns>
        public LinkedConnectionDataset(
            Uri connectionsLink,
            Uri locationsUri
        ) : this()
        {
            var conProv = new ConnectionProvider
            (connectionsLink,
                connectionsLink + "{?departureTime}");
            ConnectionsProvider = conProv;

            // Create the locations provider

            var proc = new JsonLdProcessor(new Downloader(), locationsUri);
            var loc = new LocationFragment(locationsUri);
            loc.Download(proc);
            LocationProvider = loc;
        }


        /// <summary>
        /// Adds all connection data between the given dates into the databases.
        /// Locations are only added as needed
        /// </summary>
        public (int loaded, int reused) AddAllConnectionsTo(TransitDbWriter writer,
            DateTime start, DateTime end)
        {
            return writer.AddAllConnections(this, start, end);
        }

        /// <summary>
        /// Adds all location data into the database
        /// </summary>
        public void AddAllLocationsTo(TransitDbWriter writer)
        {
            writer.AddAllLocations(this);
        }



        public void UpdateTimeFrame(TransitDbWriter w, DateTime start, DateTime end
        )
        {
            Log.Information($"Loading time window {start}->{end}");
            AddAllConnectionsTo(w, start, end);
        }
    }
}