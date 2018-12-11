using System;

namespace Itinero.Transit.Data
{
    public static class ConnectionExtensions
    {

        public static String ToString(this IConnection c)
        {
            return $"Connection {c.Id} from {c.DepartureStop} ({DateTimeExtensions.FromUnixTime(c.DepartureTime):HH:mm})" +
                   $" to {c.ArrivalStop} ({DateTimeExtensions.FromUnixTime(c.ArrivalTime):HH:mm})";

        }
        
    }
}