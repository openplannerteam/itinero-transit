namespace Itinero.Transit.IO.GTFS
{
    /// <summary>
    /// Contains settings to customize the loading of a GTFS feed.
    /// </summary>
    public class GTFSLoadSettings
    {
        /// <summary>
        /// A flag to add unused stops.
        /// </summary>
        public bool AddUnusedStops { get; set; } = false;

        /// <summary>
        /// A flag to add unused trips.
        /// </summary>
        public bool AddUnusedTrips { get; set; } = false;
    }
}