using Itinero.Transit.Data.Attributes;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// Contains extension methods for the transit db.
    /// </summary>
    public static class TransitDbExtensions
    {
        /// <summary>
        /// Adds a new stop and returns it's internal id.
        /// </summary>
        /// <param name="writer">The transit db.</param>
        /// <param name="globalId">The global stop id.</param>
        /// <param name="longitude">The stop longitude.</param>
        /// <param name="latitude">The stop latitude.</param>
        /// <param name="attributes">The stop attributes.</param>
        /// <returns>An internal id representing the stop in this transit db.</returns>
        public static (uint tileId, uint localId) AddOrUpdateStop(this TransitDb.TransitDbWriter writer, string globalId, double longitude,
            double latitude, params Attribute[] attributes)
        {
            return writer.AddOrUpdateStop(globalId, longitude, latitude, attributes);
        }
        
        /// <summary>
        /// Adds a new trip and returns it's internal id.
        /// </summary>
        /// <param name="writer">The db.</param>
        /// <param name="globalId">The global id.</param>
        /// <param name="attributes">The attributes.</param>
        /// <returns>An internal id representing the trip in this transit db.</returns>
        public static uint AddOrUpdateTrip(this TransitDb.TransitDbWriter writer, string globalId, params Attribute[] attributes)
        {
            return writer.AddOrUpdateTrip(globalId, attributes);
        }
    }
}