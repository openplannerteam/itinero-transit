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
    public class Profile : IConnectionsProvider, ILocationProvider
    {
        internal readonly IConnectionsProvider ConnectionsProvider;
        internal readonly ILocationProvider LocationProvider;

        /// <summary>
        /// Indicates the radius within which stops are searched during the
        /// profile scan algorithms.
        ///
        /// Every stop that is reachable along the way is used to search stops close by 
        /// </summary>
        internal int IntermodalStopSearchRadius = 250;

        internal int EndpointSearchRadius = 500;

        internal Profile(IConnectionsProvider connectionsProvider,
            ILocationProvider locationProvider)
        {
            ConnectionsProvider = connectionsProvider;
            LocationProvider = locationProvider;
        }


        /// <summary>
        ///  Creates a default profile, based on the locationsfragment-URL and conenctions-location fragment 
        /// </summary>
        /// <returns></returns>
        internal Profile(
            string profileName,
            Uri connectionsLink,
            Uri locationsFragment,
            LocalStorage storage,
            Downloader loader = null
        )
        {
            loader = loader ?? new Downloader();

            storage = storage?.SubStorage(profileName);

            var conProv = new LinkedConnectionProvider
            (connectionsLink,
                connectionsLink + "{?departureTime}",
                loader);

            ConnectionsProvider = storage == null
                ? (IConnectionsProvider) conProv
                : new LocallyCachedConnectionsProvider(conProv, storage.SubStorage("timetables"));

            // Create the locations provider


            var locProc = new JsonLdProcessor(loader, locationsFragment);

            LocationProvider =
                storage == null
                    ? (ILocationProvider) new LocationsFragment(locationsFragment)
                    : new CachedLocationsFragment(
                        locationsFragment,
                        locProc,
                        storage.SubStorage("locations")
                    );

        }


        public Location GetCoordinateFor(Uri locationId)
        {
            return LocationProvider.GetCoordinateFor(locationId);
        }

        public bool ContainsLocation(Uri locationId)
        {
            return LocationProvider.ContainsLocation(locationId);
        }


        public IEnumerable<Location> GetLocationByName(string name)
        {
            return LocationProvider.GetLocationByName(name);
        }

        public IEnumerable<Location> GetAllLocations()
        {
            return LocationProvider.GetAllLocations();
        }

        public ITimeTable GetTimeTable(Uri id)
        {
            return ConnectionsProvider.GetTimeTable(id);
        }

        public Uri TimeTableIdFor(DateTime includedTime)
        {
            return ConnectionsProvider.TimeTableIdFor(includedTime);
        }

    }
}