using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Functional")]
namespace Itinero.Transit
{
    /// <summary>
    /// Contains extension methods to handle unix time.
    /// </summary>
    public static class DateTimeExtensions
    {
        private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private const ulong _secondsInADay = 24 * 60 * 60;

        /// <summary>
        /// Converts a number of milliseconds from 1/1/1970 into a standard DateTime.
        /// </summary>
        public static DateTime FromUnixTime(this ulong seconds)
        {
            return _epoch.AddSeconds(seconds); // to a multiple of 100 nanosec or ticks.
        }
    
        /// <summary>
        /// Converts a standard DateTime into the number of seconds since 1/1/1970.
        /// </summary>
        public static ulong ToUnixTime(this DateTime date)
        {
            return (ulong) (date - _epoch).TotalSeconds; // from a multiple of 100 nanosec or ticks to milliseconds.
        }

        /// <summary>
        /// Extracts the date component.
        /// </summary>
        /// <param name="seconds">The unix time in seconds.</param>
        /// <returns></returns>
        internal static ulong ExtractDate(ulong seconds)
        {
            return (seconds - (seconds % _secondsInADay));
        }

        /// <summary>
        /// Jumps to the next day.
        /// </summary>
        /// <param name="seconds">The unix time in seconds.</param>
        /// <returns></returns>
        internal static ulong AddDay(ulong seconds)
        {
            var date = FromUnixTime(seconds);
            date = date.AddDays(1);
            return date.ToUnixTime();
        }

        /// <summary>
        /// Jumps to the previous day.
        /// </summary>
        /// <param name="seconds">The unix time in seconds.</param>
        /// <returns></returns>
        internal static ulong RemoveDay(ulong seconds)
        {
            var date = FromUnixTime(seconds);
            date = date.AddDays(-1);
            return date.ToUnixTime();
        }
    }
}