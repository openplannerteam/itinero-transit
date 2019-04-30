using Itinero.Transit.Data.Attributes;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// Contains extension methods for the trips db.
    /// </summary>
    public static class TripsDbExtensions
    {
        /// <summary>
        /// Adds a new trip and returns it's internal id.
        /// </summary>
        /// <param name="tripsDb">The db.</param>
        /// <param name="globalId">The global id.</param>
        /// <param name="attributes">The attributes.</param>
        /// <returns>An internal id representing the trip in this transit db.</returns>
        public static TripId Add(this TripsDb tripsDb, string globalId, params Attribute[] attributes)
        {
            return tripsDb.Add(globalId, attributes);
        }
    }
}