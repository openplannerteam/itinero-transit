using System;
using System.Collections.Generic;
using JsonLD.Core;

namespace Itinero.IO.LC
{
    /// <summary>
    /// A profile represents the preferences of the traveller.
    /// Which PT-operators does he want to take? Which doesn't he?
    /// How fast does he walk? All these are stored here
    /// </summary>
    public class Profile<T> : IConnectionsProvider, IFootpathTransferGenerator, ILocationProvider
        where T : IJourneyStats<T>
    {
        public readonly IConnectionsProvider ConnectionsProvider;
        public readonly ILocationProvider LocationProvider;
        public readonly IFootpathTransferGenerator FootpathTransferGenerator;

        public readonly T StatsFactory;
        public readonly ProfiledStatsComparator<T> ProfileCompare;
        public readonly StatsComparator<T> ParetoCompare;

        /// <summary>
        /// Indicates the radius within which stops are searched during the
        /// profile scan algorithms.
        ///
        /// Every stop that is reachable along the way is used to search stops close by 
        /// </summary>
        public int IntermodalStopSearchRadius = 250;

        public int EndpointSearchRadius = 500;

        public Profile(IConnectionsProvider connectionsProvider,
            ILocationProvider locationProvider,
            IFootpathTransferGenerator footpathTransferGenerator,
            T statsFactory,
            ProfiledStatsComparator<T> profileCompare,
            StatsComparator<T> paretoCompare)
        {
            ConnectionsProvider = connectionsProvider;
            LocationProvider = locationProvider;
            FootpathTransferGenerator = footpathTransferGenerator;
            StatsFactory = statsFactory;
            ProfileCompare = profileCompare;
            ParetoCompare = paretoCompare;
        }


        /// <summary>
        ///  Creates a default profile, based on the locationsfragment-URL and conenctions-location fragment 
        /// </summary>
        /// <returns></returns>
        public Profile(
            string profileName,
            Uri connectionsLink,
            Uri locationsFragment,
            string routerDbPath,
            LocalStorage storage,
            T statsFactory,
            ProfiledStatsComparator<T> profileCompare,
            StatsComparator<T> paretoCompare,
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
            ProfileCompare = profileCompare;
            ParetoCompare = paretoCompare;
        }

        public Journey<T> CalculateEas(Uri departure, Uri arrival, DateTime departureTime, DateTime endTime)
        {
            var eas = new EarliestConnectionScan<T>(departure, arrival, departureTime, endTime, this);
            return eas.CalculateJourney();
        }

        // ReSharper disable once UnusedMember.Global
        public IEnumerable<IContinuousConnection> CloseByGenesisConnections(Location around, int radius,
            DateTime genesisTime)
        {
            var result = new List<IContinuousConnection>();

            var close = GetLocationsCloseTo(around.Lat, around.Lon, radius);
            foreach (var uri in close)
            {
                result.Add(new WalkingConnection(uri, genesisTime));
            }

            return result;
        }

        public IEnumerable<IContinuousConnection> WalkToCloseByStops(DateTime departureTime, Location from, int radius)
        {
            var close = LocationProvider.GetLocationsCloseTo(from.Lat, from.Lon, radius);
            var result = new HashSet<IContinuousConnection>();
            foreach (var stop in close)
            {
                var transfer = FootpathTransferGenerator.GenerateFootPaths(departureTime,
                    from, LocationProvider.GetCoordinateFor(stop));
                result.Add(transfer);
            }

            return result;
        }


        public IEnumerable<IContinuousConnection> WalkFromCloseByStops(DateTime arrivalTime,
            Location to, int radius)
        {
            var close = LocationProvider.GetLocationsCloseTo(to.Lat, to.Lon, radius);
            var result = new HashSet<IContinuousConnection>();
            foreach (var stop in close)
            {
                var transfer = FootpathTransferGenerator.GenerateFootPaths(arrivalTime,
                    LocationProvider.GetCoordinateFor(stop), to);
                if (transfer == null ||
                    Equals(transfer.DepartureLocation(), transfer.ArrivalLocation()))
                {
                    continue;
                }

                var diff = transfer.ArrivalTime() - transfer.DepartureTime();
                transfer = transfer.MoveTime(-diff.TotalSeconds);
                result.Add(transfer);
            }

            return result;
        }

        /// <summary>
        /// Given two connections (e.g. within the same station; or to a bus station which is close by),
        /// calculates an object representing the transfer (e.g. walking from platform 2 to platform 5; or walking 250 meters)
        /// </summary>
        /// <param name="from">The connection that the newly calculated connection continues on</param>
        /// <param name="to">The connection that should be taken after the returned connection</param>
        /// <returns>A connection representing the transfer. Returns null if no transfer is possible (e.g. to little time)</returns>
        public IJourneyPart CalculateInterConnection(IJourneyPart from, IJourneyPart to)
        {
            var footpath = FootpathTransferGenerator.GenerateFootPaths(
                from.ArrivalTime(),
                LocationProvider.GetCoordinateFor(from.ArrivalLocation()),
                LocationProvider.GetCoordinateFor(to.DepartureLocation()));

            if (footpath.ArrivalTime() > to.DepartureTime())
            {
                // we can't make it in time to the connection where we are supposed to go
                return null;
            }

            return footpath;
        }

        public Location GetCoordinateFor(Uri locationId)
        {
            return LocationProvider.GetCoordinateFor(locationId);
        }

        public bool ContainsLocation(Uri locationId)
        {
            return LocationProvider.ContainsLocation(locationId);
        }

        public IEnumerable<Uri> GetLocationsCloseTo(float lat, float lon, int radiusInMeters)
        {
            return LocationProvider.GetLocationsCloseTo(lat, lon, radiusInMeters);
        }

        public BoundingBox BBox()
        {
            return LocationProvider.BBox();
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

        public IContinuousConnection GenerateFootPaths(DateTime departureTime, Location from, Location to)
        {
            return FootpathTransferGenerator.GenerateFootPaths(departureTime, from, to);
        }

        /// <summary>
        /// Creates a shallow copy of the profile, with one difference:
        /// the TransferGenerator is wrapped into a memoizing generator
        /// </summary>
        /// <returns></returns>
        public Profile<T> MemoizingPathsProfile()
        {
            return new Profile<T>(
                ConnectionsProvider,
                LocationProvider,
                new MemoizingTransferGenerator(FootpathTransferGenerator),
                StatsFactory,
                ProfileCompare,
                ParetoCompare
            );
        }
    }
}