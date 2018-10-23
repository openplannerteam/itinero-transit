namespace Itinero_Transit.CSA
{
    /// <summary>
    /// A profile represents the preferences of the traveller.
    /// Which PT-operators does he want to take? Which doesn't he?
    /// How fast does he walk? All these are stored here
    /// </summary>
    public class Profile<T> where T : IJourneyStats<T>
    {
        public readonly IConnectionsProvider ConnectionsProvider;
        public readonly ILocationProvider LocationProvider;
        public readonly IFootpathTransferGenerator FootpathTransferGenerator;

        public readonly T StatsFactory;
        public readonly  StatsComparator<T> ProfileCompare, ParetoCompare;
        
        public Profile(IConnectionsProvider connectionsProvider,
            ILocationProvider locationProvider,
            IFootpathTransferGenerator footpathTransferGenerator,
            T statsFactory,
            StatsComparator<T> profileCompare,
            StatsComparator<T> paretoCompare)
        {
            ConnectionsProvider = connectionsProvider;
            LocationProvider = locationProvider;
            FootpathTransferGenerator = footpathTransferGenerator;
            StatsFactory = statsFactory;
            ProfileCompare = profileCompare;
            ParetoCompare = paretoCompare;
        }
    }
}