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
    public class Profile<T> : IConnectionsProvider, IFootpathTransferGenerator, ILocationProvider
        where T : IJourneyStats<T>
    {
        internal readonly IConnectionsProvider ConnectionsProvider;
        internal readonly ILocationProvider LocationProvider;

        internal readonly T StatsFactory;
        //public readonly ProfiledStatsComparator<T> ProfileCompare;

        /// <summary>
        /// Indicates the radius within which stops are searched during the
        /// profile scan algorithms.
        ///
        /// Every stop that is reachable along the way is used to search stops close by 
        /// </summary>
        internal int IntermodalStopSearchRadius = 250;

        internal int EndpointSearchRadius = 500;

        internal Profile(IConnectionsProvider connectionsProvider,
            ILocationProvider locationProvider,
            T statsFactory)
        {
            ConnectionsProvider = connectionsProvider;
            LocationProvider = locationProvider;
            StatsFactory = statsFactory;
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
            T statsFactory,
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

            // Intermediate transfer generator
            // The OsmTransferGenerator will reuse an existing routerdb if it is already loaded
            // TODO: remove all links to Itinero and routing on road networks.
            //FootpathTransferGenerator = new OsmTransferGenerator(routerDbPath);

            // The other settings 
            StatsFactory = statsFactory;
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