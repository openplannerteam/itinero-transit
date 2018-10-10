using System.ComponentModel.DataAnnotations;
using Itinero;
using Itinero.Profiles;
using Vehicle = Itinero.Osm.Vehicles.Vehicle;

namespace Itinero_Transit.CSA.ConnectionProviders
{
    /// <summary>
    /// This class uses the Itinero routeplanner to calculate distances
    /// and thus time needed between two locations.
    /// </summary>
    public class WalkingTransferRouter
    {
        private readonly RouterDb _routerDb;
        private readonly Profile _profile;

        /// <summary>
        /// Create a new walkingtransferrouter for the given country.
        /// </summary>
        /// <param name="routerDb">The graph for the country in which we want to calculate routes</param>
        /// <param name="profile">The profile of the pedestrian walking from stop to stop. If null or none given, will default to Itinero.OSM.Vehicle.Pedestrian.Fastest</param>
        public WalkingTransferRouter(RouterDb routerDb, Profile profile = null)
        {
            _routerDb = routerDb;
            _profile = profile ?? Vehicle.Pedestrian.Fastest();
        }

        public void CreateRoutingMatrix()
        {
            
        }
    }
}