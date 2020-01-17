using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Itinero.Transit.Tests")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Benchmarks")]
[assembly: InternalsVisibleTo("Itinero.Transit.Tests.Functional")]
namespace Itinero.Transit.Utils
{
    /// <summary>
    /// Contains extension methods to handle unix time.
    /// </summary>
    public static class DateTimeExtensions
    {
        private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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
            if (date.Kind != DateTimeKind.Utc)
            {
                throw new FormatException("Please, only provide DateTimes in UTC format");
            }
            return (ulong) (date - _epoch).TotalSeconds; // from a multiple of 100 nanosec or ticks to milliseconds.
        }


        public static DateTime ConvertToUtcFrom(this DateTime dateTime, TimeZoneInfo originTimeZone)
        {
            if (dateTime.Kind != DateTimeKind.Unspecified)
            {
                throw new ArgumentException("To convert a foreign time zone in UTC, it should be entered as unspecified");
            }
            var tzOffset = originTimeZone.GetUtcOffset(dateTime);

            var parsedDateTimeZone = new DateTimeOffset(dateTime, tzOffset);
            // There must be a better way to do this...
            return new DateTime(parsedDateTimeZone.ToUniversalTime().DateTime.Ticks, DateTimeKind.Utc);
        }

        public static DateTime ConvertTo(this DateTime dateTime, TimeZoneInfo targetTimeZone)
        {
            if (dateTime.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("To convert a datetime into a foreign time zone, it should be entered as UTC");
            }
            var tzOffset = targetTimeZone.GetUtcOffset(dateTime);

            var parsedDateTimeZone = new DateTimeOffset(
                new DateTime(dateTime.Ticks, DateTimeKind.Unspecified), -tzOffset);
            // There must be a better way to do this...
            return new DateTime(parsedDateTimeZone.ToUniversalTime().DateTime.Ticks, DateTimeKind.Unspecified);
        }
    }
}