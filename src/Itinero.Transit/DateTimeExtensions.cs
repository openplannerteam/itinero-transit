// The MIT License (MIT)

// Copyright (c) 2018 Anyways B.V.B.A.

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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
    internal static class DateTimeExtensions
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public const ulong SecondsInADay = 24 * 60 * 60;

        /// <summary>
        /// Converts a number of milliseconds from 1/1/1970 into a standard DateTime.
        /// </summary>
        public static DateTime FromUnixTime(ulong seconds)
        {
            return Epoch.AddSeconds(seconds); // to a multiple of 100 nanosec or ticks.
        }
    
        /// <summary>
        /// Converts a standard DateTime into the number of seconds since 1/1/1970.
        /// </summary>
        public static ulong ToUnixTime(this DateTime date)
        {
            return (ulong) (date - Epoch).TotalSeconds; // from a multiple of 100 nanosec or ticks to milliseconds.
        }

        /// <summary>
        /// Extracts the date component.
        /// </summary>
        /// <param name="seconds">The unix time in seconds.</param>
        /// <returns></returns>
        public static ulong ExtractDate(ulong seconds)
        {
            return (seconds - (seconds % SecondsInADay));
        }

        /// <summary>
        /// Jumps to the next day.
        /// </summary>
        /// <param name="seconds">The unix time in seconds.</param>
        /// <returns></returns>
        public static ulong AddDay(ulong seconds)
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
        public static ulong RemoveDay(ulong seconds)
        {
            var date = FromUnixTime(seconds);
            date = date.AddDays(-1);
            return date.ToUnixTime();
        }
    }
}