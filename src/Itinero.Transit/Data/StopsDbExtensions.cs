using Itinero.Transit.Data.Attributes;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// Contains extension methods for the stops db.
    /// </summary>
    public static class StopsDbExtensions
    {
        /// <summary>
        /// Adds a new stop and returns it's internal id.
        /// </summary>
        /// <param name="stopsDb">The stops db.</param>
        /// <param name="globalId">The global stop id.</param>
        /// <param name="longitude">The stop longitude.</param>
        /// <param name="latitude">The stop latitude.</param>
        /// <param name="attributes">The stop attributes.</param>
        /// <returns>An internal id representing the stop in this transit db.</returns>
        public static (uint tileId, uint localId) Add(this StopsDb stopsDb, string globalId, double longitude, double latitude, params Attribute[] attributes)
        {
            return stopsDb.Add(globalId, longitude, latitude, attributes);
        }
    }
}