using System;
using System.Collections.Generic;
using Itinero.Transit.IO.LC.CSA.ConnectionProviders;
using Itinero.Transit.IO.LC.CSA.LocationProviders;
using Itinero.Transit.IO.LC.CSA.Utils;
using JsonLD.Core;

namespace Itinero.Transit.IO.LC.CSA
{
    /// <summary>
    /// A profile represents the preferences of the traveller.
    /// Which PT-operators does he want to take? Which doesn't he?
    /// How fast does he walk? All these are stored here
    /// </summary>
    public class Profile
    {
        internal readonly List<ConnectionProvider> ConnectionsProvider;
        internal readonly List<LocationProvider> LocationProvider;

        /// <summary>
        /// Indicates the radius within which stops are searched during the
        /// profile scan algorithms.
        ///
        /// Every stop that is reachable along the way is used to search stops close by 
        /// </summary>
        internal int IntermodalStopSearchRadius = 250;

        internal int EndpointSearchRadius = 500;

        internal Profile(ConnectionProvider[] connectionsProvider,
            LocationProvider[] locationProvider)
        {
            ConnectionsProvider = new List<ConnectionProvider>(connectionsProvider);
            LocationProvider = new List<LocationProvider>(locationProvider);
        }

        internal Profile(List<Profile> sources)
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
        internal Profile(Uri connectionsLink,
            Uri locationsUri,
            Downloader loader = null
        )
        {
            loader = loader ?? new Downloader();


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
    }
}