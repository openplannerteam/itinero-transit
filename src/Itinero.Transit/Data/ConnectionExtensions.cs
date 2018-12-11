using System;

namespace Itinero.Transit.Data
{
    /// <summary>
    /// Contains extension methods related to connections.
    /// </summary>
    public static class ConnectionExtensions
    {
        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <param name="c">The connection.</param>
        /// <returns>A string that represents the current object.</returns>
        public static string ToString(this IConnection c)
        {
            return $"Connection {c.Id} from {c.DepartureStop} ({DateTimeExtensions.FromUnixTime(c.DepartureTime):HH:mm})" +
                   $" to {c.ArrivalStop} ({DateTimeExtensions.FromUnixTime(c.ArrivalTime):HH:mm})";
        }
    }
}